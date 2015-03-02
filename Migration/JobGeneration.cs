using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace EsccWebTeam.Cms.Migration
{
   public class JobGeneration
    {
        /*
         var credentials = new NetworkCredential(ConfigurationManager.AppSettings["cmsAccountName"],
                         ConfigurationManager.AppSettings["cmsAccountPassword"]);
                     var handler = new HttpClientHandler {Credentials = credentials};
        
         */

       public void CreateJob(string guid, string command, string target)
       {
           if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["JobApi"]))
           {
               throw new ConfigurationErrorsException("URL not set in appSettings/JobUri");
           }

           var request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["JobApi"]);
           request.ContentType = "application/json";
           request.Method = "POST";

           using (var streamWriter = new StreamWriter(request.GetRequestStream()))
           {
               var job = new JobRequest()
               {
                   PageGuid = guid,
                   Command = command,
                   DeploymentTarget = target
               };

               var json = JsonConvert.SerializeObject(job);

               streamWriter.Write(json);
           }

           var response = (HttpWebResponse)request.GetResponse();
           using (var streamReader = new StreamReader(response.GetResponseStream()))
           {
               var result = streamReader.ReadToEnd();
              
           }
       }

    }

    public partial class JobRequest
    {
        public string PageGuid { get; set; }
        public string DeploymentTarget { get; set; }
        public string Command { get; set; }
    }
}
