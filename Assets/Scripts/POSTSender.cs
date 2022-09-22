using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

public class POSTSender {

    private IPAddress ip;
    private int port;
    private string endpoint;
    private IEnumerable<SourceManager> sources;
    private string uri;

    public POSTSender(IPAddress ip, int port, string endpoint, IEnumerable<SourceManager> sources) {
        this.ip = ip;
        this.port = port;
        this.sources = sources;
        this.endpoint = endpoint;
        uri = $"{ip.ToString()}:{port}{endpoint}";
    }

    public void SendFrame() {
        var dataDict = new Dictionary<string, byte[]>();
        foreach(var source in sources) {
            dataDict.Add(source.Name, source._Data);
        }
        string json = JsonConvert.SerializeObject(dataDict);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

        using(UnityWebRequest webRequest = new UnityWebRequest(this.uri, "POST")) {
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SendWebRequest();
        }
    }

}