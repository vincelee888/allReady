﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AllReady.Controllers;
using AllReady.Models;
using AllReady.ViewModels;
using AllReady.Features.Activity;
using AllReady.Features.Notifications;
using AllReady.UnitTest.Extensions;
using MediatR;
using Microsoft.AspNet.Authorization;
using Moq;
using Xunit;
using Microsoft.AspNet.Mvc;

namespace AllReady.UnitTest.Controllers
{
    //GetQrCode: check notes for this one

    public class ActivityApiControllerTest
    {
        [Fact]
        public void GetReturnsActivitiesWitUnlockedCampaigns()
        {
            var activities = new List<Activity>
            {
                new Activity { Id = 1, Campaign = new Campaign { Locked = false }},
                new Activity { Id = 2, Campaign = new Campaign { Locked = true }}
            };

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.Activities).Returns(activities);

            var sut = new ActivityApiController(dataAccess.Object, null);
            var results = sut.Get().ToList();
            
            Assert.Equal(activities[0].Id, results[0].Id);
        }

        [Fact]
        public void GetReturnsCorrectModel()
        {
            var sut = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null);
            var results = sut.Get().ToList();
            Assert.IsType<List<ActivityViewModel>>(results);
        }

        //TODO: come back to these two tests until you hear back from Tony Surma about returning null instead of retruning HttpNotFound
        //GetByIdReturnsNullWhenActivityIsNotFoundById ???
        //[Fact]
        //public void GetByIdReturnsHttpNotFoundWhenActivityIsNotFoundById()
        //{
        //    var controller = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null)
        //        .SetFakeUser("1");

        //    var result = controller.Get(It.IsAny<int>());
        //    Assert.IsType<HttpNotFoundResult>(result);
        //}

        [Fact]
        public void GetByIdReturnsCorrectViewModel()
        {
            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(new Activity { Campaign = new Campaign() });

            var sut = new ActivityApiController(dataAccess.Object, null);
            var result = sut.Get(It.IsAny<int>());

            Assert.IsType<ActivityViewModel>(result);
        }

        [Fact]
        public void GetActivitiesByPostalCodeCallsActivitiesByPostalCodeWithCorrectPostalCodeAndMiles()
        {
            const string zip = "zip";
            const int miles = 100;

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.ActivitiesByPostalCode(It.IsAny<string>(), It.IsAny<int>())).Returns(new List<Activity>());

            var sut = new ActivityApiController(dataAccess.Object, null);
            sut.GetActivitiesByPostalCode(zip, miles);

            dataAccess.Verify(x => x.ActivitiesByPostalCode(zip, miles), Times.Once);
        }

        [Fact]
        public void GetActivitiesByPostalCodeReturnsCorrectViewModel()
        {
            var sut = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null);
            var result = sut.GetActivitiesByPostalCode(It.IsAny<string>(), It.IsAny<int>());

            Assert.IsType<List<ActivityViewModel>>(result);
        }

        [Fact]
        public void GetActivitiesByGeographyCallsActivitiesByGeographyWithCorrectLatitudeLongitudeAndMiles()
        {
            const double latitude = 1;
            const double longitude = 2;
            const int miles = 100;

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.ActivitiesByGeography(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>())).Returns(new List<Activity>());

            var sut = new ActivityApiController(dataAccess.Object, null);
            sut.GetActivitiesByGeography(latitude, longitude, miles);

            dataAccess.Verify(x => x.ActivitiesByGeography(latitude, longitude, miles), Times.Once);
        }

        [Fact]
        public void GetActivitiesByGeographyReturnsCorrectViewModel()
        {
            var sut = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null);
            var result = sut.GetActivitiesByGeography(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>());

            Assert.IsType<List<ActivityViewModel>>(result);
        }

        [Fact]
        public void GetCheckinReturnsHttpNotFoundWhenUnableToFindActivityByActivityId()
        {
            var sut = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null);
            var result = sut.GetCheckin(It.IsAny<int>());
            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public void GetCheckinReturnsTheCorrectViewModel()
        {
            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(new Activity());

            var sut = new ActivityApiController(dataAccess.Object, null);
            var result = (ViewResult)sut.GetCheckin(It.IsAny<int>());

            Assert.IsType<Activity>(result.ViewData.Model);
        }

        [Fact]
        public void GetCheckinReturnsTheCorrectView()
        {
            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(new Activity());

            var sut = new ActivityApiController(dataAccess.Object, null);
            var result = (ViewResult)sut.GetCheckin(It.IsAny<int>());

            Assert.Equal("NoUserCheckin", result.ViewName);
        }

        [Fact]
        public async Task PutCheckinReturnsHttpNotFoundWhenUnableToFindActivityByActivityId()
        {
            var sut = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null);
            var result = await sut.PutCheckin(It.IsAny<int>());

            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public async Task PutCheckinCallsGetActivityWithCorrectActivityId()
        {
            const int activityId = 1;

            var dataAccess = new Mock<IAllReadyDataAccess>();
            var sut = new ActivityApiController(dataAccess.Object, null);
            await sut.PutCheckin(activityId);

            dataAccess.Verify(x => x.GetActivity(activityId), Times.Once);
        }

        [Fact]
        public async Task PutCheckinCallsAddActivitySignupAsyncWithCorrectDataWhenUsersSignedUpisNotNullAndCheckinDateTimeIsNull()
        {
            const string userId = "userId";
            var utcNow = DateTime.UtcNow;

            var activity = new Activity();
            var activitySignup = new ActivitySignup { User = new ApplicationUser { Id = userId }};
            activity.UsersSignedUp.Add(activitySignup);

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(activity);

            
            var sut = new ActivityApiController(dataAccess.Object, null) { DateTimeUtcNow = () => utcNow }
                .SetFakeUser(userId);
            await sut.PutCheckin(It.IsAny<int>());

            dataAccess.Verify(x => x.AddActivitySignupAsync(activitySignup), Times.Once);
            dataAccess.Verify(x => x.AddActivitySignupAsync(It.Is<ActivitySignup>(y => y.CheckinDateTime == utcNow)), Times.Once);
        }

        [Fact]
        public async Task PutCheckinReturnsCorrectJsonWhenUsersSignedUpIsNotNullAndCheckinDateTimeIsNull()
        {
            const string userId = "userId";

            var activity = new Activity { Name = "ActivityName", Description = "ActivityDescription" };
            var activitySignup = new ActivitySignup { User = new ApplicationUser { Id = userId } };
            activity.UsersSignedUp.Add(activitySignup);

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(activity);

            var sut = new ActivityApiController(dataAccess.Object, null)
                .SetFakeUser(userId);

            var expected = $"{{ Activity = {{ Name = {activity.Name}, Description = {activity.Description} }} }}";

            var result = (JsonResult)await sut.PutCheckin(It.IsAny<int>());

            Assert.IsType<JsonResult>(result);
            Assert.Equal(expected, result.Value.ToString());
        }

        [Fact]
        public async Task PutCheckinReturnsCorrectJsonWhenUsersSignedUpIsNullAndCheckinDateTimeIsNotNull()
        {
            const string userId = "userId";

            var activity = new Activity { Name = "ActivityName", Description = "ActivityDescription" };

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivity(It.IsAny<int>())).Returns(activity);

            var sut = new ActivityApiController(dataAccess.Object, null)
                .SetFakeUser(userId);

            var expected = $"{{ NeedsSignup = True, Activity = {{ Name = {activity.Name}, Description = {activity.Description} }} }}";

            var result = (JsonResult)await sut.PutCheckin(It.IsAny<int>());

            Assert.IsType<JsonResult>(result);
            Assert.Equal(expected, result.Value.ToString());
        }

        [Fact]
        public async Task RegisterActivityReturnsHttpBadRequetWhenSignupModelIsNull()
        {
            var sut = new ActivityApiController(null, null);
            var result = await sut.RegisterActivity(null);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task RegisterActivityReturnsCorrectJsonWhenModelStateIsNotValid()
        {
            const string modelStateErrorMessage = "modelStateErrorMessage";

            var sut = new ActivityApiController(null, null);
            sut.AddModelStateError(modelStateErrorMessage);

            var jsonResult = (JsonResult)await sut.RegisterActivity(new ActivitySignupViewModel());
            var result = jsonResult.GetValueForProperty<List<string>>("errors");

            Assert.IsType<JsonResult>(jsonResult);
            Assert.IsType<List<string>>(result);
            Assert.Equal(result.First(), modelStateErrorMessage);
        }

        [Fact]
        public async Task RegisterActivitySendsActivitySignupCommandAsyncWithCorrectData()
        {
            var model = new ActivitySignupViewModel();
            var mediator = new Mock<IMediator>();

            var sut = new ActivityApiController(null, mediator.Object);
            await sut.RegisterActivity(model);

            mediator.Verify(x => x.SendAsync(It.Is<ActivitySignupCommand>(command => command.ActivitySignup.Equals(model))));
        }

        [Fact]
        public async Task RegisterActivityReturnsHttpStatusResultOfOk()
        {
            var sut = new ActivityApiController(null, Mock.Of<IMediator>());
            var result = (HttpStatusCodeResult)await sut.RegisterActivity(new ActivitySignupViewModel());

            Assert.IsType<HttpStatusCodeResult>(result);
            Assert.Equal(result.StatusCode, (int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task UnregisterActivityReturnsHttpNotFoundWhenUnableToGetActivitySignupByActivitySignupIdAndUserId()
        {
            var controller = new ActivityApiController(Mock.Of<IAllReadyDataAccess>(), null)
                .SetFakeUser("1");

            var result = await controller.UnregisterActivity(It.IsAny<int>());
            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public async Task UnregisterActivityGetActivitySignUpIsCalledWithCorrectActivityIdAndUserId()
        {
            const int activityId = 1;
            const string userId = "1";
            
            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess.Setup(x => x.GetActivitySignup(It.IsAny<int>(), It.IsAny<string>())).Returns(new ActivitySignup { Activity = new Activity(), User = new ApplicationUser() });

            var controller = new ActivityApiController(dataAccess.Object, Mock.Of<IMediator>())
                .SetFakeUser(userId);
            
            await controller.UnregisterActivity(activityId);

            dataAccess.Verify(x => x.GetActivitySignup(activityId, userId), Times.Once);
        }

        [Fact]
        public async Task UnregisterActivityPublishesUserUnenrollsWithCorrectData()
        {
            const int activityId = 1;
            const string applicationUserId = "applicationUserId";

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess
                .Setup(x => x.GetActivitySignup(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(new ActivitySignup
                {
                    Activity = new Activity { Id = activityId },
                    User = new ApplicationUser { Id = applicationUserId }
                });

            var mediator = new Mock<IMediator>();

            var controller = new ActivityApiController(dataAccess.Object, mediator.Object)
                .SetFakeUser("1");

            await controller.UnregisterActivity(activityId);

            mediator.Verify(mock => mock.PublishAsync(It.Is<UserUnenrolls>(ue => ue.ActivityId == activityId && ue.UserId == applicationUserId)), Times.Once);
        }

        [Fact]
        public async Task UnregisterActivityDeleteActivityAndTaskSignupsAsyncIsCalledWithCorrectActivitySignupId()
        {
            const int activitySignupId = 1;

            var dataAccess = new Mock<IAllReadyDataAccess>();
            dataAccess
                .Setup(x => x.GetActivitySignup(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(new ActivitySignup
                {
                    Id = activitySignupId,
                    Activity = new Activity (),
                    User = new ApplicationUser()
                });

            var controller = new ActivityApiController(dataAccess.Object, Mock.Of<IMediator>())
                .SetFakeUser("1");

            await controller.UnregisterActivity(2);

            dataAccess.Verify(x => x.DeleteActivityAndTaskSignupsAsync(activitySignupId), Times.Once);
        }

        [Fact]
        public void ControllerHasRouteAtttributeWithTheCorrectRoute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributes().OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "api/activity");
        }

        [Fact]
        public void ControllerHasProducesAtttributeWithTheCorrectContentType()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributes().OfType<ProducesAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.ContentTypes.Select(x => x.MediaType).First(), "application/json");
        }

        [Fact]
        public void GetHasHttpGetAttribute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.Get()).OfType<HttpGetAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
        }

        [Fact]
        public void GetByIdHasHttpGetAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.Get(It.IsAny<int>())).OfType<HttpGetAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "{id}");
        }

        [Fact]
        public void GetByIdHasProducesAttributeWithCorrectContentTypes()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.Get(It.IsAny<int>())).OfType<ProducesAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Type, typeof(ActivityViewModel));
            Assert.Equal(attribute.ContentTypes.Select(x => x.MediaType).First(), "application/json");
        }

        [Fact]
        public void GetActivitiesByZipHasRouteAttributeWithRoute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.GetActivitiesByPostalCode(It.IsAny<string>(), It.IsAny<int>())).OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "search");
        }

        [Fact]
        public void GetActivitiesByLocationHasRouteAttributeWithCorrectRoute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.GetActivitiesByGeography(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int>())).OfType<RouteAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "searchbylocation");
        }

        [Fact]
        public void GetQrCodeHasHttpGetAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.GetQrCode(It.IsAny<int>())).OfType<HttpGetAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "{id}/qrcode");
        }

        [Fact]
        public void GetCheckinHasHttpGetAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = sut.GetAttributesOn(x => x.GetCheckin(It.IsAny<int>())).OfType<HttpGetAttribute>().SingleOrDefault();
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "{id}/checkin");
        }

        [Fact]
        public void PutCheckinHasHttpPutAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (HttpPutAttribute)sut.GetAttributesOn(x => x.PutCheckin(It.IsAny<int>())).SingleOrDefault(x => x.GetType() == typeof(HttpPutAttribute));
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "{id}/checkin");
        }

        [Fact]
        public void PutCheckinHasAuthorizeAttribute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (AuthorizeAttribute)sut.GetAttributesOn(x => x.PutCheckin(It.IsAny<int>())).SingleOrDefault(x => x.GetType() == typeof(AuthorizeAttribute));
            Assert.NotNull(attribute);
        }

        [Fact]
        public void RegisterActivityHasValidateAntiForgeryTokenAttribute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (ValidateAntiForgeryTokenAttribute)sut.GetAttributesOn(x => x.RegisterActivity(It.IsAny<ActivitySignupViewModel>())).SingleOrDefault(x => x.GetType() == typeof(ValidateAntiForgeryTokenAttribute));
            Assert.NotNull(attribute);
        }

        [Fact]
        public void RegisterActivityHasHttpPostAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (HttpPostAttribute)sut.GetAttributesOn(x => x.RegisterActivity(It.IsAny<ActivitySignupViewModel>())).SingleOrDefault(x => x.GetType() == typeof(HttpPostAttribute));
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "signup");
        }

        [Fact]
        public void UnregisterActivityHasHttpDeleteAttributeWithCorrectTemplate()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (HttpDeleteAttribute)sut.GetAttributesOn(x => x.UnregisterActivity(It.IsAny<int>())).SingleOrDefault(x => x.GetType() == typeof(HttpDeleteAttribute));
            Assert.NotNull(attribute);
            Assert.Equal(attribute.Template, "{id}/signup");
        }

        [Fact]
        public void UnregisterActivityHasAuthorizeAttribute()
        {
            var sut = new ActivityApiController(null, null);
            var attribute = (AuthorizeAttribute)sut.GetAttributesOn(x => x.UnregisterActivity(It.IsAny<int>())).SingleOrDefault(x => x.GetType() == typeof(AuthorizeAttribute));
            Assert.NotNull(attribute);
        }
    }
}

