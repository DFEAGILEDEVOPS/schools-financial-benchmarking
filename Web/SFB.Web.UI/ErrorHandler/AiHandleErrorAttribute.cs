using System;using System.Web.Mvc;using Microsoft.ApplicationInsights;namespace SFB.Web.UI.ErrorHandler{    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]     public class AiHandleErrorAttribute : HandleErrorAttribute    {        public override void OnException(ExceptionContext filterContext)        {            if (filterContext != null && filterContext.HttpContext != null && filterContext.Exception != null)            {                //If customError is Off, then AI HTTPModule will report the exception                if (filterContext.HttpContext.IsCustomErrorEnabled)                {
                    if (filterContext.RequestContext.HttpContext.Response.StatusCode != 404)
                    {
                        var ai = new TelemetryClient();
                        ai.TrackException(filterContext.Exception);
                        ai.TrackTrace($"URL: {filterContext.RequestContext.HttpContext.Request.RawUrl}");
                        ai.TrackTrace($"FORM VARIABLES: {filterContext.RequestContext.HttpContext.Request.Form}");
                    }                }             }            //base.OnException(filterContext);        }    }}