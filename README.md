# VOICE: Voice-Operated Interactive Conversation Engine
* Uses Microsoft Cognitive Services to perform speech to text and text to speech.
* Uses ChatGPT API to generate response from the gpt-4o model.
## In order to use VOICE
* You will need to set up Cognitive Services in Azure
* Create a key vault in Azure
* Add keys for Cognitive Services and OpenAI API
* Modify appsettings.json with these values
* Assure appsettings.json is listed in your .gitignore before checking in any code.
* Assure appsettings.json is listed in your .gitignore before checking in any code.

* appsettings.json Example
```
{
  "AzureSpeech": {
    "Region": "<Your_Region>", // e.g. "eastus"
    "Secret": "<Your_Secret_Name_In_KeyVault>"
  },
  "OpenAI":{
    "ApiKey": "<Your_Secret_Name_In_KeyVault>"
  },
  "AzureKeyVault": {
    "Url": "Your_KeyVault_URL", // e.g. "https://<Your_KeyVault_Name>.vault.azure.net/"
  }
}
```