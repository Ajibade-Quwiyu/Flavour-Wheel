using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class DataGetter : MonoBehaviour
{
    public TMP_Text resultText; // Reference to the TextMeshPro object in your scene

    // Call this method to initiate the search
    public void StartSearch(string query)
    {
        StartCoroutine(SearchRequest(query));
    }

    private IEnumerator SearchRequest(string query)
    {
        string searchUrl = $"https://api.duckduckgo.com/?q={UnityWebRequest.EscapeURL(query)}&format=json&pretty=1";
        UnityWebRequest request = UnityWebRequest.Get(searchUrl);

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        // Check for errors
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error: {request.error}");
            resultText.text = "Error: Could not connect or retrieve data.";
        }
        else
        {
            // Parse the response (you may need to adjust this based on the API's response format)
            string jsonResponse = request.downloadHandler.text;
            ProcessResponse(jsonResponse);
        }
    }

    private void ProcessResponse(string jsonResponse)
    {
        // Example of simple processing and displaying the result
        resultText.text = jsonResponse;
    }
}
