using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace UpdateCheckCS
{
    class UpdateCheck : IDisposable
    {
        //Initialize values with default since the get methods can be called at any time
        string errorString = "0";
        string successString = "0";
        string startURL = "0";
        bool latestBool = true;
        /// <summary>
        /// Preforms the check with the Software Server of whether the currently running app is up to date
        /// </summary>
        /// <param name="v">Version. Write the version number that is latest in software server</param>
        /// <param name="ad">App Id. This number will never change</param>
        /// <param name="bu">Base URL to be used when launching browser to update. If the full URL is set inside the software server, write 'na'</param>
        /// <returns>True if no error is encountered. False if error is encountered. UpdateCheck.error method will return the error.</returns>
        public bool tryCheck(string v, int ad, string bu)
        {
            string currentVersion = v;
            string softwareID = ad.ToString();
            //Build string that will be used in search query
            string versionStringBase = "http://api.simpleaob.com/v1/software";
            string finalVersionString = (versionStringBase + ("?id=" + (softwareID + ("&version=" + currentVersion))));
            // Make request to software server
            try
            {
                WebRequest getVersion = WebRequest.Create(finalVersionString);
                using (Stream responseVersion = getVersion.GetResponse().GetResponseStream())
                {
                    using (StreamReader responseStream = new StreamReader(responseVersion))
                    {
                        dynamic jsonData = responseStream.ReadToEnd();
                        JObject jsonRead = JObject.Parse(jsonData);
                        if (((string)jsonRead["error"] == "0"))
                        {
                            if (((bool)jsonRead["is_latest"] == false))
                            {
                                latestBool = false;
                                if (bu != "na")
                                {
                                    startURL = bu + jsonRead["kh_id"];
                                }
                                else
                                {
                                    startURL = (string)jsonRead["kd_id"];
                                }
                                successString = "Newer version found: v" + (string)jsonRead["software_latest_version"];
                                return true;
                            }
                            else
                            {
                                successString = "No newer version of the application was found";
                                return true;
                            }
                        }
                        else
                        {
                            errorString = (string)jsonRead["error"];
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorString = ex.Message;
                return false;
            }
        }
        /// <summary>
        /// Returns the error string if one is encountered
        /// </summary>
        /// <returns></returns>
        public string error()
        {
            return errorString;
        }
        /// <summary>
        /// Returns the success string if no error is encountered
        /// </summary>
        /// <returns></returns>
        public string success()
        {
            return successString;
        }
        /// <summary>
        /// Returns the URL to launch the browser with, or whatever else you want to do with it
        /// </summary>
        /// <returns></returns>
        public string URL()
        {
            return startURL;
        }
        /// <summary>
        /// Returns whether the current version is the latest or not
        /// </summary>
        /// <returns></returns>
        public bool latest()
        {
            return latestBool;
        }
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
    }
}
