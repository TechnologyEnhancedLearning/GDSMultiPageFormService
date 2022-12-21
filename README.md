# GDSMultiPageFormService
.Net Core MVC service to implement GDS style multi-page forms

## Introduction
The NHS Design System (and GDS) suggest that complex transactional journeys (see Transactional journeys in this link) should be broken down into steps with one (or sometimes two related) pieces of information being asked of the user on each page.

At the end of the journey, we should show a check your answers page summarising user responses before they submit them (usually to the database) with links to go back and change our answers.

To implement this in .Net, we need to retain the user's responses across the multi-page form.

The multi-page form service provides a mechanism for doing this.

## The problem
The most obvious place to store users' responses before they are committed to the database is in TempData. TempData uses browser cookies to store data, though, and when we are storing a complex data model (lots of questions/options across multiple pages) the cookie can quickly become too large, causing the browser to crash with a “Bad request” error.

## The solution
The multi-page form service uses the application database to store the transactional data as a JSON string against a GUID so that it can be created, updated and retrieved throughout the transactional journey.

![image](https://user-images.githubusercontent.com/67740339/208898341-ce0c45b2-1dc0-47e2-8ab8-47f7e380463c.png)

The table used to store the temp data

The NuGet package will create this table if it doesn’t already exist.

When a user gets to the end of the transactional journey and submits their choices, the associated MultiPageFormData record is deleted.

_A job or function should be set up to delete MultiPageFormData records (for example that are more than 7 days old) to ensure records relating to transactions journeys that are not completed are tidied up._

## Usage
### Starting a transactional flow
A controller method should be created to start the multi-page transactional flow. This should:
1. Clear TempData
2. Invoke the multi-page form service, passing it the model for the data being captured
3. Redirect to an action that will return the view for the first page of the transaction

#### For example:
```
[HttpGet("AddCourseNew")]
public IActionResult AddCourseNew()
{
//1. Clear Tempdata:
    TempData.Clear();
//2. Invoke the multi-page form service, passing it the model for the data being captured:
    multiPageFormService.SetMultiPageFormData(
        new AddNewCentreCourseTempData(),
        MultiPageFormDataFeature.AddNewCourse,
        TempData
    );
//3. Redirect to an action that will return the view for the first page of the transaction:
    return RedirectToAction("SelectCourse");
}
```
### Retrieve the data for each step of the flow
Each step of the flow will have a controller get method which should:
1. Use the multipage form service to retrieve transactional data
2. Populate the view model with user''s selections from the data
3. Return the view

#### For example:
```
[HttpGet("AddCourse/SelectCourse")]
        public IActionResult SelectCourse(
            string? categoryFilterString = null,
            string? topicFilterString = null
        )
        {
//1. Use the multipage form service to retrieve transactional data:
            var data = multiPageFormService.GetMultiPageFormData<AddNewCentreCourseTempData>(
                MultiPageFormDataFeature.AddNewCourse,
                TempData
            );
//2. Populate the view model with users selections from the data:
            var model = GetSelectCourseViewModel(
                categoryFilterString ?? data.CategoryFilter,
                topicFilterString ?? data.TopicFilter,
                data.Application?.ApplicationId
            );
//3. Return the view:
            return View("AddNewCentreCourse/SelectCourse", model);
        }
```
### Update the data on submit for each step of the flow
On submit their selection for each step of the form, the POST method should:
1. Use the multipage form service to retrieve transactional data
2. Update the data with the selections submitted by the user
3. Store the updated data using the multipage form service
4. Redirect to the GET action method for the next step in the transaction

#### For example:
```
[HttpPost("AddCourse/SelectCourse")]
        public IActionResult SelectCourse(
            int? applicationId,
            string? categoryFilterString = null,
            string? topicFilterString = null
        )
        {
//1. Use the multipage form service to retrieve transactional data:
            var data = multiPageFormService.GetMultiPageFormData<AddNewCentreCourseTempData>(
                MultiPageFormDataFeature.AddNewCourse,
                TempData
            );

            if (applicationId == null)
            {
                ModelState.AddModelError("ApplicationId", "Select a course");
                return View(
                    "AddNewCentreCourse/SelectCourse",
                    GetSelectCourseViewModel(
                        categoryFilterString,
                        topicFilterString
                    )
                );
            }

            var centreId = User.GetCentreId();
            var categoryId = User.GetAdminCourseCategoryFilter();

            var selectedApplication =
                courseService.GetApplicationOptionsAlphabeticalListForCentre(centreId, categoryId)
                    .Single(ap => ap.ApplicationId == applicationId);
//2. Update the data with the selections submitted by the user:
            data.CategoryFilter = categoryFilterString;
            data.TopicFilter = topicFilterString;
            data!.SetApplicationAndResetModels(selectedApplication);
//3. Store the updated data using the multipage form service:
            multiPageFormService.SetMultiPageFormData(data, MultiPageFormDataFeature.AddNewCourse, TempData);
//4: Redirect to the GET action method for the next step in the transaction:
            return RedirectToAction("SetCourseDetails");
        }
```        

### Show a summary page at the end of the flow
The GET method for the summary page, should:
1. Use the multipage form service to retrieve transactional data
2. Populate the view model with user's selections from the data
3. Return the view

The view should show all of the selections made by the user with “Change” links allowing the user to return to any step in the transaction. See NHS Design System prototype example.

#### For example:
```
[HttpGet("AddCourse/Summary")]
        public IActionResult Summary()
        {
//1. Use the multipage form service to retrieve transactional data:
            var data = multiPageFormService.GetMultiPageFormData<AddNewCentreCourseTempData>(
                MultiPageFormDataFeature.AddNewCourse,
                TempData
            );
//2. Populate the view model with user's selections from the data
            var model = new SummaryViewModel(data!);
//3. Return the view
            return View("AddNewCentreCourse/Summary", model);
        }
```
### Handle submitting the data
The POST method for the summary page, triggered by submitting, should:
1. Use the multipage form service to retrieve transactional data
2. Commit the data to the database (using an update or insert service method or API call)
3. Use the multipage form service to remove the transactional data
4. Clear TempData
5. Redirect to a confirmation screen

#### For example:
```
[HttpPost("AddCourse/Summary")]
        public IActionResult? CreateNewCentreCourse()
        {
//1. Use the multipage form service to retrieve transactional data
            var data = multiPageFormService.GetMultiPageFormData<AddNewCentreCourseTempData>(
                MultiPageFormDataFeature.AddNewCourse,
                TempData
            );

            using var transaction = new TransactionScope();

            var customisation = GetCustomisationFromTempData(data!);
//2. Commit the data to the database (using an update or insert service method or API call):
            var customisationId = courseService.CreateNewCentreCourse(customisation);

...

//3. Use the multipage form service to remove the transactional data:
            multiPageFormService.ClearMultiPageFormData(MultiPageFormDataFeature.AddNewCourse, TempData);

            transaction.Complete();
//4. Clear TempData
            TempData.Clear();
            TempData.Add("customisationId", customisationId);
            TempData.Add("applicationName", data.Application!.ApplicationName);
            TempData.Add("customisationName", data.CourseDetailsData!.CustomisationName);
//5. Redirect to a confirmation screen
            return RedirectToAction("Confirmation");
        }
```
