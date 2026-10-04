using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GameEventSender : MonoBehaviour
{
    [SerializeField] private string eventEndpoint = "http://localhost:5000/api/events";

    [Serializable]
    private class GameEventData
    {
        public string childId;
        public string eventType;
        public string region;
        public string puzzleType;
        public string value;
    }

    public void SendEvent(
        string childId,
        string eventType,
        string region,
        string puzzleType,
        string value)
    {
        GameEventData eventData = new GameEventData
        {
            childId = childId,
            eventType = eventType,
            region = region,
            puzzleType = puzzleType,
            value = value
        };

        StartCoroutine(PostEvent(eventData));
    }

    private IEnumerator PostEvent(GameEventData eventData)
    {
        string json = JsonUtility.ToJson(eventData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(eventEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Lumora eventi gönderildi: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError(
                    "Lumora eventi gönderilemedi. HTTP " + request.responseCode +
                    " - " + request.error + "\n" + request.downloadHandler.text
                );
            }
        }
    }
}
