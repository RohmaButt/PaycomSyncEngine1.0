using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Http;

namespace Afiniti.PaycomEngine.Controllers
{
    public class EchoController : ApiController
    {
        public string Get()
        {
            return "Yo bro:)";
        }
    }
}