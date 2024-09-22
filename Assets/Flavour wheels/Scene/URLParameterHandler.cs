using UnityEngine;
using System;

public class URLParameterHandler : MonoBehaviour
{
    public GameObject adminObject;
    public GameObject userObject;

    void Start()
    {
        // Deactivate both objects by default
        adminObject.SetActive(false);
        userObject.SetActive(false);

        // Get the URL parameters
        string url = Application.absoluteURL;
        Uri uri = new Uri(url);
        string query = uri.Query;

        // Parse the query string
        if (query.StartsWith("?"))
        {
            query = query.Substring(1);
        }
        string[] queryParams = query.Split('&');
        foreach (string param in queryParams)
        {
            string[] keyValue = param.Split('=');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0];
                string value = keyValue[1];

                if (key == "mode")
                {
                    if (value == "admin")
                    {
                        adminObject.SetActive(true);
                        Debug.Log("Admin mode activated");
                    }
                    else if (value == "user")
                    {
                        userObject.SetActive(true);
                        Debug.Log("User mode activated");
                    }
                }
            }
        }

        if (!adminObject.activeSelf && !userObject.activeSelf)
        {
            Debug.Log("No specific mode activated");
        }
    }
}