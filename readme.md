# Peplink SP Tool

This is a simple command line executable that manages the Service Provider defaults flag for Peplink devices via InControl2.
Use of this flag sets a default configuration on the device which is used in place of the standard factory default config. This feature can be dangerous.

More information on the API endpoints in use can be found here:
https://incontrol2.peplink.com/api/ic2-api-doc#!post_rest_o_o_g_o_sp_default

### Setup

As per the configuration for API access on InControl2, create a OAuth2 client in your dashboard for the application to authenticate against:
```
Initiation necessitates the acquisition of an OAuth 2.0 client ID. For applications accessing exclusively an individual's InControl account, such an ID may be obtained by navigating to the "Client Application" section under the user's profile within InControl (In organization/group overview, click at your email address at the top right corner. Then scroll down to the very bottom, you should see "Client Applications").
```

Please see the included file `ClientApp Setup.png` in the repository for the required setup.

Place the ClientID and ClientSecret in the `appsettings.json` file like so:
```
{
    "PeplinkSPTool":
    {
        "Endpoint": "https://api.ic.peplink.com",
        "ClientID": "YourClientIDHere",
        "ClientSecret": "YourClientSecretHere"
    }
}
```

### Usage

The application can be invoked from your commandline. As long as the configuration file can be found, your default browser will be invoked to perform OAuth against your logged in session for InControl2