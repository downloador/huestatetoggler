using System;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

HttpClient client = new HttpClient();
string APIUrl = "http://192.168.0.84/api/47JHc0dCl0ecAImOPrXoJheViaHFoVZ3FX4kvbZd/";

List<int> Lights = new List<int> { };
string LightsAsString = "3"; // Later implementation

foreach (char LightID in LightsAsString)
{
    Lights.Add((int) LightID - '0');
}

async Task<JObject> Test(string type, string requestURL, string body="")
{
    if (type == "GET")
    {
        JObject parsedJSON = JObject.Parse(await client.GetStringAsync(APIUrl + requestURL));
        return parsedJSON;
    }
    else if (type == "POST")
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(APIUrl + requestURL, content);

        var responseString = await response.Content.ReadAsStringAsync();

        Console.WriteLine(responseString);

        JObject parsedJSON = JObject.Parse(responseString);
        return parsedJSON;
    }
    else if (type == "PUT")
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(APIUrl + requestURL, content);

        var responseString = await response.Content.ReadAsStringAsync();

        Console.WriteLine(responseString);

        JArray parsedJSON = JArray.Parse(responseString);
        return (JObject)parsedJSON[0];
    }
    else if (type == "DELETE")
    {
        return JObject.FromObject(new { err = "DELETE unsupported" });
    }

    return JObject.FromObject(new { err = "Missing type, must be either GET, POST, PUT" });
}

//

NotifyIcon notifyIcon = new NotifyIcon();

notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
notifyIcon.Visible = true;
notifyIcon.Text = Application.ProductName;

notifyIcon.MouseClick += nIBTC;

async void nIBTC(object sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Left) return;

    // Stupid ass approach, but whatever I'm using whatever the light's last state is as default
    bool LightState = false;

    foreach (var LightID in Lights)
    {
        var responseJSON = await Test("GET", "lights/" + LightID.ToString());
        bool thisLightState = (bool)responseJSON["state"]["on"];

        LightState = thisLightState;
    }
    // And we loop through it again... great
    foreach (var LightID in Lights)
    {
        await Test("PUT", "lights/" + LightID.ToString() + "/state", "{\"on\":" + (!LightState == true ? "true" : "false") + "}");
    }

    Console.WriteLine("Set all lights to " + (!LightState == true ? "true" : "false"));
}

var contextMenu = new ContextMenuStrip();
contextMenu.Items.Add("Exit Application", null, (s, e) => { Process.GetCurrentProcess().Kill(); });
contextMenu.Items.Add("Turn off", null, async (s, e) => {
    foreach (var LightID in Lights)
    {
        await Test("PUT", "lights/" + LightID.ToString() + "/state", "{\"on\":false}");
    }

    Console.WriteLine("Set all lights to false");
});
contextMenu.Items.Add("Turn on", null, async (s, e) => {
    foreach (var LightID in Lights)
    {
        await Test("PUT", "lights/" + LightID.ToString() + "/state", "{\"on\":true}");
    }

    Console.WriteLine("Set all lights to true");
});
notifyIcon.ContextMenuStrip = contextMenu;

new Thread(() =>
{
    Thread.CurrentThread.IsBackground = true;

    for (int i = 0; i < 5; i++)
    {
        Console.WriteLine("Hiding Console window in " + Convert.ToString(5 - i));
        Thread.Sleep(1000);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    var handle = GetConsoleWindow();
    ShowWindow(handle, 0);
}).Start();

Application.Run();