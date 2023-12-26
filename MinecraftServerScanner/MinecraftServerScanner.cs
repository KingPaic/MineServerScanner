using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Forms;
using System.Threading;

public class MinecraftServerScanner
{
    ProgressBar  curpg;
    ListView  curlw;
    public bool running = false;
    int totalPorts;
    int completedPorts = 0;

   
    SemaphoreSlim semaphore = new SemaphoreSlim(100);
    
    /// <summary>
    /// Start New Search
    /// </summary>
    /// <param name="ipAddresses">ips to use (check server.pro)</param>
    /// <param name="startPort">Begin TCP</param>
    /// <param name="endPort">Last TCP</param>
    /// <param name="pg"></param>
    /// <param name="lw"></param>
    /// <param name="threadmax"> max thread</param>
    /// <returns></returns>
    public async Task<Dictionary<string, string>> ScanServersAsync(string[] ipAddresses, int startPort, int endPort, ProgressBar pg,ListView lw, int threadmax=100)
    {
        if (running) { return null; }
        semaphore = new SemaphoreSlim(threadmax);

        running = true;
        curlw = lw;
        curpg = pg;
        var serverVersions = new Dictionary<string, string>();
        var tasks = new List<Task>();

         totalPorts = (endPort - startPort + 1) * ipAddresses.Length;

       
            for (int port = startPort; port <= endPort; port++)
            {
               foreach (var ipAddress in ipAddresses)
                 {
                string endpoint = $"{ipAddress}:{port}";

                // Wait for semaphore before starting a new task
                await semaphore.WaitAsync();

                tasks.Add(ScanServerAsync(endpoint, serverVersions));

            }
        }

        await Task.WhenAll(tasks.ToArray());
        // Ensure progress reaches 100%
        UpdatePG(100);

        running = false;
        return serverVersions;
    }


    /// <summary>
    /// Update percentage
    /// </summary>
    /// <param name="perc"></param>
    private void UpdatePG(int perc) {
        try
        {
            if (curpg.InvokeRequired)
            {
                // Invoke required, switch to UI thread
                curpg.Invoke(new Action(() => curpg.Value = perc));
            }
            else
            {
                // No invoke required, update directly
                curpg.Value = perc;
            }
        }
        catch { }
      
    }


    /// <summary>
    /// add element to listviews
    /// </summary>
    /// <param name="sdata">tcp data to add</param>
    private void AddElementToListView(string sdata)
    {
   

        string[] s = sdata.Split('|');
        s[1] = s[1].Replace("\0", "|");
        string[] sdip = s[0].Split(':');
        string[] sd = s[1].Split('|');

        ListViewItem lwi= new ListViewItem(sdip[0]);
        lwi.SubItems.Add(sdip[1]);
        lwi.SubItems.Add(sd[2]);
        lwi.SubItems.Add(sd[4]);
        lwi.SubItems.Add(sd[3]);
        if (curlw.InvokeRequired)
        {
            // Invoke required, switch to UI thread
            curlw.Invoke(new Action(() => curlw.Items.Add(lwi)));
        }
        else
        {
            // No invoke required, update directly
            curlw.Items.Add(lwi);
        }

    }

    /// <summary>
    /// Scann specific ip & udp
    /// </summary>
    /// <param name="endpoint">ip:UDP</param>
    /// <param name="serverVersions">return</param>
    /// <returns></returns>
    private async Task ScanServerAsync(string endpoint, Dictionary<string, string> serverVersions)
    {
        string[] parts = endpoint.Split(':');
        if (parts.Length != 2)
            return;

        string ipAddress = parts[0];
        int port = int.Parse(parts[1]);

        try
        {
            MinecraftServerQuery serverQuery = new MinecraftServerQuery(ipAddress, port);
         
            string serverVersion = await serverQuery.GetServerVersionAsync();
            if (serverVersion != null) {
                serverVersions[endpoint] = serverVersion;
                AddElementToListView($"{endpoint}|{serverVersion}");
                Console.WriteLine($"Server {endpoint}: {serverVersion}"); 
            }
        }
        catch (Exception ex)
        {
            serverVersions[endpoint] = null;

        }

        completedPorts++;
        int percentComplete = (int)((double)completedPorts / totalPorts * 100);
        UpdatePG(percentComplete);

        // Release the semaphore immediately after starting the task
        semaphore.Release();


    }
}


/// <summary>
/// Ping and receive TCP ack buffer
/// </summary>
public class MinecraftServerQuery
{
    private string serverIpAddress;
    private int serverPort;
    


    public MinecraftServerQuery(string ipAddress, int port = 25565)
    {
        this.serverIpAddress = ipAddress;
        this.serverPort = port;
    }

    public async Task<string> GetServerVersionAsync()
    {
        string ret = "";
        try
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(serverIpAddress, serverPort);

                using (var stream = client.GetStream())
                {
                    byte[] queryPacket = new byte[] { 0xFE, 0x01 };
                    await stream.WriteAsync(queryPacket, 0, queryPacket.Length);

                    byte[] responseData = new byte[4096];
                    int bytesRead = await stream.ReadAsync(responseData, 0, responseData.Length);

                    return Encoding.Unicode.GetString(responseData, 2, bytesRead - 2);
                    client.Close();
                    client.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            return null;
        }



    }

    /// <summary>
    /// NOT USED
    /// </summary>
    public class ListViewItemComparer : System.Collections.IComparer
    {
        private int column;
        private SortOrder sortOrder;

        public ListViewItemComparer()
        {
            column = 0; // Default to sorting the first column
            sortOrder = SortOrder.Ascending; // Default to ascending order
        }

        public ListViewItemComparer(int column, SortOrder order)
        {
            this.column = column;
            this.sortOrder = order;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            // You should implement error checking here if the columns don't exist or have incorrect data

            // Get the sub-item text to compare
            string textX = itemX.SubItems[column].Text;
            string textY = itemY.SubItems[column].Text;

            // Perform a comparison based on the sort order
            int compareResult = string.Compare(textX, textY);

            // Invert the result for descending order
            if (sortOrder == SortOrder.Descending)
            {
                compareResult = -compareResult;
            }

            return compareResult;
        }
    }

}