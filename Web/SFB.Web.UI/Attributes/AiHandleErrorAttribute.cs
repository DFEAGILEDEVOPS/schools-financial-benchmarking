using System;

namespace SFB.Web.UI.ErrorHandler
                    if (filterContext.RequestContext.HttpContext.Response.StatusCode != 404)
                    {
                        var ai = new TelemetryClient();
                        ai.TrackException(filterContext.Exception);
                        ai.TrackTrace($"IP ADDRESS: {filterContext.RequestContext.HttpContext.Request.UserHostAddress}");
                        ai.TrackTrace($"HTTP REFERRER: {filterContext.RequestContext.HttpContext.Request.UrlReferrer}");
                        ai.TrackTrace($"BROWSER: {filterContext.RequestContext.HttpContext.Request.Browser.Browser}");
                        ai.TrackTrace($"URL: {filterContext.RequestContext.HttpContext.Request.RawUrl}");
                        ai.TrackTrace($"FORM VARIABLES: {filterContext.RequestContext.HttpContext.Request.Form}");