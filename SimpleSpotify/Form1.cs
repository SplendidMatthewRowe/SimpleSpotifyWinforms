using System;
using System.Collections.Generic;
using System.Data;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Net.Http.Headers;
using SimpleSpotify.Models;
using System.Linq;

namespace SimpleSpotify
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Global vars (not ideal)
        string clientId = "PUT YOUR ID HERE";
        string clientSecret = "PUT YOUR SECRET HERE";
        string accessToken = "unknown";
        string tokenEndpoint = "https://accounts.spotify.com/api/token";
        string baseAddress = "https://api.spotify.com/v1";  //Found this on https://developer.spotify.com/documentation/web-api/howtos/web-app-profile

        //Could be set by form or passed as parameters
        string countryCode = "gb";
        string limit = "20";  //max number of songs


        private void Form1_Load(object sender, EventArgs e)
        {
            //Generate token on form load
            using (WebClient client = new WebClient())
            {
                // Prepare the request body
                var requestBody = new NameValueCollection
                {
                    { "grant_type", "client_credentials" },
                };

                // Create the Basic Authorization header
                string authorizationHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                client.Headers[HttpRequestHeader.Authorization] = $"Basic {authorizationHeader}";

                try
                {
                    // Send the POST request to the token endpoint
                    byte[] responseBytes = client.UploadValues(tokenEndpoint, "POST", requestBody);
                    string responseJson = Encoding.UTF8.GetString(responseBytes);

                    //Assign the token
                    dynamic jsonResponse = JsonSerializer.Deserialize<SimpleSpotify.Models.AuthResult>(responseJson);
                    accessToken = jsonResponse.access_token;

                    // Display the response to the user
                    MessageBox.Show(accessToken, "Here is the Access Token: ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (WebException ex)
                {
                    MessageBox.Show($"Request failed with error: {ex.Message}");
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            //Calls the function to request the data
            IEnumerable<Models.Release> topSongs = await GetTopUKSongs();

            //topSongs now contains ALL of the album data including the pictures (see Release class for format)

            //Add album and artist to the listbox
            foreach (var song in topSongs)
            {
                lstSongs.Items.Add(song.Name + " by " + song.Artists);
                pictureBox1.ImageLocation = song.ImageUrl;
            }

            //Add pic1 to the picturebox just to show you can do this, you can change this to be more impressive
            //Maybe change on a mouse click etc...
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Load(topSongs.ElementAt(0).ImageUrl);

        }

       

        private async Task<IEnumerable<Release>> GetTopUKSongs()
        {
            //Function returns the top 20 UK albums, you can change the countryCode and Limit to adjust all this if you like

            //Prepare the http client object
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);  //Add the accessToken and the word bearer
            
            //Summon the data (access token is inside the client) (baseAddress/countryCode etc is all declared above)
            var response = await client.GetAsync($"{baseAddress}/browse/new-releases?country={countryCode}&limit={limit}");

            //Has it been succesful?
            if (response.IsSuccessStatusCode)
            {

                //response.EnsureSuccessStatusCode();
                var responseStream = await response.Content.ReadAsStreamAsync();

                //Deserialize response into a C# object you can do something with!
                GetNewReleaseResult responseObject = await JsonSerializer.DeserializeAsync<GetNewReleaseResult>(responseStream);
                    
                //Push the data into a "Release" class see the class definition (usually over there-->)
                return responseObject.albums.items.Select(i => new Models.Release
                {
                    Name = i.name,
                    Date = i.release_date,
                    ImageUrl = i.images.FirstOrDefault().url,
                    Link = i.external_urls.spotify,
                    Artists = i.artists[0].name  //Just take the first artist's name
                });

                
            }
            else
            {
                //Sad times, it didn't work!
                MessageBox.Show("Failed to retrieve top songs. Please check your access token and try again.");
            }

            return null;
            }
        
    }
}

