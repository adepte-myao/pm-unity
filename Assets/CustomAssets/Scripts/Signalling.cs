using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SignallingClient
{
    private const string host = "http://192.168.137.1:8080";
    private string token;
    private string userID;
    private string otherUser;
    private string subscriptionID;
    private OnMessage subscribeProcessor;
    private bool cancelling;

    public SignallingClient(string userID, OnMessage subscribeProcessor)
    {
        this.subscribeProcessor = subscribeProcessor;
        this.userID = userID;
        cancelling = false;
    }

    public void SetOtherUser(string otherUserID)
    {
        this.otherUser = otherUserID;
    }

    public async Task Login(string username, string passHash)
    {
        var entity = new LoginRequest
        {
            userID = username,
            passHash = passHash
        };

        var encoded = JsonUtility.ToJson(entity);
        var request = UnityWebRequest.Post(host + "/login", encoded, "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();
        await request.SendWebRequest();
        if (request.responseCode != 200)
        {
            Debug.Log(request.error);
        }

        var response = request.downloadHandler.text;
        Debug.Log(response);
        var result = JsonUtility.FromJson<LoginResponse>(response);
        token = result.token;
    }

    public async Task JoinChannel(string channelName)
    {
        var entity = new ChannelRequest
        {
            channelName = channelName
        };

        var encoded = JsonUtility.ToJson(entity);
        var request = UnityWebRequest.Post(host + "/channel/join", encoded, "application/json");
        request.SetRequestHeader("Authorization", token);
        await request.SendWebRequest();
        if (request.responseCode != 200)
        {
            Debug.Log(request.responseCode);
            Debug.Log(request.error);
        }
        var response = request.downloadHandler.text;
        var result = JsonUtility.FromJson<ChannelResponse>(response);
        subscriptionID = result.subscriptionID;
    } 

    public IEnumerator Update(float periodSeconds)
    {
        while (!cancelling)
        {
            yield return new WaitForSeconds(periodSeconds);

            var request = UnityWebRequest.Post(host + "/channel/collect?subscriptionID="+subscriptionID, null, "application/json");
            request.SetRequestHeader("Authorization", token);
            yield return request.SendWebRequest();
            if (request.responseCode != 200)
            {
                Debug.Log(request.responseCode);
                Debug.Log(request.error);
            }
            var response = request.downloadHandler.text;
            var result = JsonConvert.DeserializeObject<JObject>(response);

            yield return null;

            for (int i = 0; i < result["entities"].Count(); i++)
            {
                var entity = result["entities"][i].ToString();
                var procSeq = subscribeProcessor(entity);
                while (procSeq.MoveNext()) {
                    yield return null;
                }
            }
        }
    }
    

    public async Task LeaveChannel(string channelName)
    {
        var entity = new
        {
            channelName = channelName
        };

        var encoded = JsonConvert.SerializeObject(entity);
        var request = UnityWebRequest.Post(host + "/channel/leave", encoded, "application/json");
        request.SetRequestHeader("Authorization", token);
        await request.SendWebRequest();
        if (request.responseCode != 200)
        {
            Debug.Log(request.error);
        }
    }

    public async Task SendMessage(string channel, object message)
    {
        var entity = new SendToPeerRequest()
        {
            channelName = channel,
            destinationUserID = this.otherUser,
            message = message
        };

        var encoded = JsonConvert.SerializeObject(entity);
        var request = UnityWebRequest.Post(host+"/peer/send", encoded, "application/json");
        request.SetRequestHeader("Authorization", token);
        Debug.Log($"peer request sending: {encoded}");
        await request.SendWebRequest();
        Debug.Log($"peer request response code: {request.responseCode}");
        if (request.responseCode != 200)
        {
            Debug.Log(request.error);
        }
    }

    public void Dispose()
    {
        cancelling = true;
    }
}

public delegate IEnumerator OnMessage(string message);

[Serializable]
public class SendToPeerRequest
{
    public string channelName;
    public string destinationUserID;
    public object message;
}

[Serializable]
public class MessageCollection
{
    public MessageEntity[] entities;
}

[Serializable]
public class MessageEntity
{
    public string time;
    public string actionType;
    public string userID;
    public Message data;
}

[Serializable]
public class Message
{
    public string messageType;
    public string content;
}

[Serializable]
public class LoginRequest
{
    public string userID;
    public string passHash;
}

[Serializable]
public class LoginResponse
{
    public string token;
}

[Serializable]
public class ChannelRequest
{
    public string channelName;
}

[Serializable]
public class ChannelResponse
{
    public string subscriptionID;
}