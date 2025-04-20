using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OverdriveServer.Tracking
{
    class Location
    {
        JObject offsetData;
        public Location(){
            // Load the offset data from the embedded resource
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "OverdriveServer.TrackData.offsetInfo.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                offsetData = JsonConvert.DeserializeObject<JObject>(json);
            }
        }
        public float CorrectOffset(int segmentID, int locationID, float offset, int numbits, bool reversed) {
            if(segmentID == 10){ return offset; } // Segment 10 is iffy, so just return the original offset
            JToken segmentData = offsetData[segmentID.ToString()];
            if(segmentData == null) {
                Console.WriteLine($"Segment data for segment {segmentID} not found.");
                return offset; // Return the original offset if segment data is not found
            }
            //find a object in the array "offsets" that has an "ids" array that contains locationID
            JToken offsetInfo = segmentData["offsets"].FirstOrDefault(x => {
                JArray idsArray = (JArray)x["ids"]; // Try to find the locationID in the array by checking each value with proper conversion
                return idsArray.Any(id => {
                    if (id.Type == JTokenType.Integer){ return id.Value<int>() == locationID; }
                    return false;
                });
            });
            if (offsetInfo != null) {
                //get the offset value from the object
                float offsetValue = (float)offsetInfo["offset"];
                offsetValue *= 965; // Convert to mm
                if (!reversed) { offsetValue = -offsetValue;  } // Reverse the offset if needed
                offset = offsetValue;
            }
            else {
                Console.WriteLine($"Location with ID {locationID} not found in segment {segmentID}.");
            }
            return offset; // Return the corrected offset (or original if no correction is needed)
        }
    }
}
