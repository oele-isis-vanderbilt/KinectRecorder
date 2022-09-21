using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class UDPSender : System.IDisposable {

    private IPAddress ip;
    private int port;
    private IPEndPoint remoteEndPoint;
    private UdpClient client;
    private SourceManager _Source;

    public UDPSender(SourceManager source, IPAddress ip, int port) {
        this.ip = ip;
        this.port = port;
        this._Source = source;

        remoteEndPoint = new IPEndPoint(this.ip, this.port);
        client = new UdpClient();
    }

    public void WriteFrame() {
        try {
            client.Send(_Source._Data, _Source._Data.Length, remoteEndPoint);
        } catch {}
    }

    public void Dispose() {
        client.Close();
    }
}
