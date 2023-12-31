using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;

public class FindIps
{


    ProgressBar curpg;
    ListView curlw;
    public bool running = false;
    SemaphoreSlim semaphore = new SemaphoreSlim(100);
    float total = 0;
    float done = 0;



    /// <summary>
    /// Update percentage
    /// </summary>
    /// <param name="perc"></param>
    private void UpdatePG(int perc)
    {
        perc = Math.Max(0, perc);
        perc = Math.Min(perc, 100);
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


        ListViewItem lwi = new ListViewItem(s[0]);
        lwi.SubItems.Add(s[1]);
        lwi.SubItems.Add(s[2]);
        lwi.SubItems.Add(s[3]);

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



    public async Task ScanIPRangeAsync(string ipList, ProgressBar pg, ListView lw, int timeout = 500, int threadmax = 100)
    {

        if (running) { return; }
        semaphore = new SemaphoreSlim(threadmax);

        running = true;
        curlw = lw;
        curpg = pg;

        List<string> openPorts = new List<string>();
        string[] ips = ipList.Split(',');
        float total = 253 * ips.Length;
        float done = 0;

        var tasks = new List<Task>();

        try
        {
            foreach (string ip in ips)
            {
                for (byte b = 0; b < 255; b++)
                {
                    var curip = ip.Split('.');
                    curip[3] = b.ToString();
                    string iptc = string.Join(".", curip);

                    // Wait for semaphore before starting a new task
                    await semaphore.WaitAsync();

                    tasks.Add(IpCheck(iptc, timeout));

                    done++;
                    UpdatePG((int)((done / total) * 100));

                }
            }
        }
        catch (Exception e) { 
        
        }
        await Task.WhenAll(tasks.ToArray());
        // Ensure progress reaches 100%
        UpdatePG(100);

        running = false;
        return;
    }


    private async Task IpCheck(string iptc, int timeout=500) {
        IPAddress ipAddress;
        if (IPAddress.TryParse(iptc.Trim(), out ipAddress))
        {
            if (await IsPortOpenAsync(ipAddress, 21, timeout))
            {
                AddElementToListView(ipAddress + "|" + "|" + "ON" + "||||");
            }
        }
        // Release the semaphore immediately after starting the task
        semaphore.Release();

    }



    private async Task<bool> IsPortOpenAsync(IPAddress ipAddress, int port, int timeout)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(ipAddress, port);
                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
                {
                    return true; // Port is open
                }
                else
                {
                    return false; // Port is not open
                }
            }
        }
        catch (Exception)
        {
            return false; // Port is not open
        }
    }
}