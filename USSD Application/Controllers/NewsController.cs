using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace USSD_Application.Controllers
{
    [RoutePrefix("api/News")]
    public class NewsController : ApiController
    {
        [Route("ussd")]
        // specify the actual route, your url will look like... localhost:8080/api/mobile/ussd...
        [HttpPost, ActionName("ussd")]
        // state that the method you intend to create is a POST 
        public HttpResponseMessage ussd([FromBody]Models.ServerResponse ServerResponse)
        {
            // declare a complex type as input parameter
            HttpResponseMessage rs;
            string response;
            string eventType = string.Empty;

            if (ServerResponse.text == null)
            {
                ServerResponse.text = "";
            }

            // loop through the server's text value to determine the next cause of action
            if (ServerResponse.text.Equals("", StringComparison.Ordinal))
            {
                NewsManagementDBEntities entity = new NewsManagementDBEntities();
                tblRequest tableRequest = new USSD_Application.tblRequest();
                tableRequest.PhoneNumber = ServerResponse.phoneNumber;
                tableRequest.ServiceCode = ServerResponse.serviceCode;
                tableRequest.SessionID = ServerResponse.sessionId;
                tableRequest.RequestStartDateTime = DateTime.Now;

                entity.tblRequests.Add(tableRequest);
                entity.SaveChanges();
                
                response = "CON Welcome to our service \n";
                response += " What type of NEWS you want today \n";
                response += " 1. Sport \n";
                response += " 2. Music \n";
                response += " 3. Movie \n";
            }
            else
            {
                string[] request = ServerResponse.text.Split('*');
                if (request.Count() == 1)
                {
                    response = "CON Please Enter Date to filter \n";
                }
                else if (request.Count() == 2)
                {
                    eventType = GetEventType(request[0]);
                    DateTime eventDate = DateTime.Parse(request[1]);

                    NewsManagementDBEntities entity = new NewsManagementDBEntities();
                    List<tblEvent> events = entity.tblEvents.Where(x => x.EventType == eventType && x.CreatedDate == eventDate).ToList();

                    if (events != null && events.Count > 0)
                    {
                        string newsTitles = string.Empty;
                        response = "CON Please Enter the CODE on the left \n";

                        foreach (tblEvent singleEvent in events)
                        {
                            response += singleEvent.ID + ". " + singleEvent.Title + " \n";
                        }
                    }
                    else
                    {
                        response = "END Invalid Code selected";
                    }
                }
                else if (request.Count() == 3)
                {
                    eventType = GetEventType(request[0]);
                    DateTime eventDate = DateTime.Parse(request[1]);
                    int eventID = int.Parse(request[2]);

                    NewsManagementDBEntities entity = new NewsManagementDBEntities();
                    tblEvent singleEvent = entity.tblEvents.Where(x => x.ID == eventID && x.EventType == eventType && x.CreatedDate == eventDate).FirstOrDefault();

                    if(singleEvent != null)
                    {
                        response = "END " + singleEvent.Description;

                        tblRequest oldRequest = entity.tblRequests.Where(x => x.SessionID == ServerResponse.sessionId).FirstOrDefault();

                        tblRequest newRequest = new tblRequest();
                        newRequest.PhoneNumber = ServerResponse.phoneNumber;
                        newRequest.ServiceCode = ServerResponse.serviceCode;
                        newRequest.SessionID = ServerResponse.sessionId;
                        newRequest.RequestStartDateTime = oldRequest.RequestStartDateTime;
                        newRequest.RequestCompleteDateTime = DateTime.Now;

                        entity.Entry(oldRequest).CurrentValues.SetValues(newRequest);
                        entity.SaveChanges();
                    }
                    else
                    {
                        response = "END Invalid Code selected";
                    }
                }
                else
                {
                    response = "END Invalid Option";
                }
            }

            rs = Request.CreateResponse(HttpStatusCode.Created, response);

            rs.Content = new StringContent(response, Encoding.UTF8, "text/plain");
            return rs;
        }

        private string GetEventType(string eventTypeCode)
        {
            if (eventTypeCode == "1")
            {
                return "Sport";
            }
            else if (eventTypeCode == "2")
            {
                return "Music";
            }
            else if (eventTypeCode == "3")
            {
                return "Movie";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
