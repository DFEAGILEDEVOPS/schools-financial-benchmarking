﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SFB.Web.UI.Controllers
{
    public class PocController : Controller
    {
        // GET: Poc
        public ActionResult LocationSearch()
        {
            return View();
        }

        // GET: Poc
        public ActionResult SchoolMap()
        {
            return View();
        }

        // GET: Poc
        public ActionResult SchoolMapWithLeafletJS()
        {
            return View();
        }

        // GET: Poc
        public ActionResult SchoolResults()
        {
            return View();
        }

        // GET: Poc
        public ActionResult SchoolResultsClustered()
        {
            return View();
        }
    }
}