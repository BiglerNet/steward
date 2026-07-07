# Facebook Login for the Web with the JavaScript SDK



This document walks you through the steps of implementing Facebook Login with the [Facebook SDK for JavaScript](https://developers.facebook.com/docs/javascript) on your webpage.

## Before You Start

You will need the following:

- A [Facebook Developer Account](https://developers.facebook.com/apps/)

- A [Facebook App ID](https://developers.facebook.com/docs/apps#register) for a website

## Basic Automatic Login Example

The following code sample shows you how to add the Facebook SDK for Javascript to your webpage, initialize the SDK, and, if you are logged in to Facebook, will display your name and email. If you are not logged into Facebook, the login dialog popup window will automatically be displayed.

The
[public_profile permission](https://developers.facebook.com/docs/permissions/reference/public_profile), which allows you to get publicly available information such as name and profile picture,
and the
[email permission](https://developers.facebook.com/docs/permissions/reference/email)
do not require app review and are automatically granted to all apps using Facebook Login.

```
<!doctype html>
<html lang="en">
  <head></head>
  <body>
    <h2>Add Facebook Login to your webpage</h2>

    <!-- Set the element id for the JSON response -->
    <p id="profile"></p>
    <button onclick="loginWithFacebook()">Log in with Facebook</button>

    <script>
      // Initialize the Facebook JavaScript SDK, but wait till it's loaded
      window.fbAsyncInit = function () {
        FB.init({
          appId: "{your-facebook-app-id}",
          xfbml: true,
          version: "{the-graph-api-version-for-your-app}",
        });
      };

      // Handle clicks on the "Log in with Facebook" button
      function loginWithFacebook() {
        FB.login(function (response) {
          if (response.authResponse) {
            console.log("Welcome!  Fetching your information.... ");
            // After successful login, we can fetch the user's information
            FB.api("/me", { fields: "name, email" }, function (response) {
              document.getElementById("profile").innerHTML =
                "Good to see you, " +
                response.name +
                ". i see your email address is " +
                response.email;
            });
          } else {
            console.log("User cancelled login or did not fully authorize.");
          }
        });
      }

      // Load the JavaScript SDK asynchronously
      (function (d, s, id) {
        var js,
          fjs = d.getElementsByTagName(s)[0];
        if (d.getElementById(id)) {
          return;
        }
        js = d.createElement(s);
        js.id = id;
        js.src = "https://connect.facebook.net/en_US/sdk.js";
        fjs.parentNode.insertBefore(js, fjs);
      })(document, "script", "facebook-jssdk");
    </script>
  </body>
</html>
```

## 1. Enable JavaScript SDK for Facebook Login {#redirecturl}

In the [App Dashboard](https://developers.facebook.com/apps), choose your app and scroll to **Add a Product** Click **Set Up** in the **Facebook Login** card. Select **Settings** in the left side navigation panel and under **Client OAuth Settings**, enter your redirect URL in the **Valid OAuth Redirect URIs** field for successful authorization.

Indicate that you are using the JavaScript SDK for login by setting the **Login with JavaScript SDK** toggle to “yes”, and enter the domain of your page that hosts the SDK in **Allowed Domains for JavaScript SDK** list. This ensures that access tokens are only returned to callbacks in authorized domains. Only https pages are supported for authentication actions with the Facebook JavaScript SDK.

## 2. Check Login Status of a Person {#checklogin}

The first step when your webpage loads is determining if a person is already logged into your webpage with Facebook Login. A call to [`FB.getLoginStatus`](https://developers.facebook.com/docs/reference/javascript/FB.getLoginStatus) starts a call to Facebook to get the login status. Facebook then calls your callback function with the results.

#### Sample Call

```
FB.getLoginStatus(function(response) {
    statusChangeCallback(response);
});
```

#### Sample JSON Response

```
{
    status: 'connected',
    authResponse: {
        accessToken: '{access-token}',
        expiresIn:'{unix-timestamp}',
        reauthorize_required_in:'{seconds-until-token-expires}',
        signedRequest:'{signed-parameter}',
        userID:'{user-id}'
    }
}
```

`status` specifies the login status of the person using the webpage. The `status` can be one of the following:

| `Status` Type | Description |
| --- | --- |
| `connected` | The person is logged into Facebook, and has logged into your webpage. |
| `not_authorized` | The person is logged into Facebook, but has not logged into your webpage. |
| `unknown` | The person is not logged into Facebook, so you don't know if they have logged into your webpage. Or [FB.logout()](#logout) was called before, and therefore, it cannot connect to Facebook. |

If the status is `connected`, the following `authResponse` parameters are included in the response:

| `authResponse` Parameters | Value |
| --- | --- |
| `accessToken` | An access token for the person using the webpage. |
| `expiresIn` | A UNIX time stamp when the token expires. Once the token expires, the person will need to login again. |
| `reauthorize_required_in` | The amount of time before the login expires, in seconds, and the person will need to login again. |
| `signedRequest` | A signed parameter that contains information about the person using your webpage. |
| `userID` | The ID of the person using your webpage. |

The JavaScript SDK automatically detects login status, so you don't need to do anything to enable this behavior.

## 3. Log a Person In {#login}

If a person opens your webpage but is not logged in or not logged in to Facebook, you can use [the Login dialog](https://developers.facebook.com/documentation/facebook-login/overview#logindialog) to prompt them to log in to both. If they are not logged into Facebook, they will first be prompted to log in, then prompted to log in to your webpage.

There are two ways to log someone in:

* The [Facebook Login Button](#loginbutton)
* The [Login Dialog](#logindialog) from the JavaScript SDK

### A. Log In with the Login Button {#loginbutton}

Customize the [Login Button](https://developers.facebook.com/documentation/facebook-login/web/login-button) and add the code to your project.

### B. Log In with the Javascript SDK Login Dialog {#logindialog}

To use your own login button, invoke the Login Dialog with a call to [`FB.login()`](https://developers.facebook.com/docs/reference/javascript/FB.login).

```
FB.login(function(response){
  // handle the response
});
```

### Ask for Additional Permissions

When a person clicks your HTML button a pop-up window with the Login dialog is shown. This dialog allows you to [ask for permission](https://developers.facebook.com/documentation/facebook-login/web/permissions) to access a person's data. The `scope` parameter can be passed along with the `FB.login()` function call. This optional parameter is a list of [permissions](https://developers.facebook.com/docs/facebook-login/permissions), separated by commas, that a person must confirm to give your webpage access to their data. Facebook Login requires dvanced public_profile permission, to be used by external users.

#### Sample Call

This example asks the person logging in if your webpage can have permission to access their public profile and email.

```
FB.login(function(response) {
  // handle the response
}, {scope: 'public_profile,email'});
```

### Handle the Login Dialog Response {#dialogresponse}

The response, either to connect or to cancel, returns an `authResponse` object to the callback specified when you called `FB.login()`. This response can be detected and handled within the `FB.login()`.

#### Sample Call

```
FB.login(function(response) {
  if (response.status === 'connected') {
    // Logged into your webpage and Facebook.
  } else {
    // The person is not logged into your webpage or we are unable to tell.
  }
});
```

## 4. Log a Person Out {#logout}

Log a person out of your webpage by attaching the JavaScript SDK function `FB.logout()` to a button or a link.

#### Sample Call

```
FB.logout(function(response) {
   // Person is now logged out
});
```

**Note: This function call may also log the person out of Facebook.**

#### Scenarios to Consider

1. A person logs into Facebook, then logs into your webpage. Upon logging out from your app, the person is still logged into Facebook.
2. A person logs into your webpage and into Facebook as part of your app's login flow. Upon logging out from your app, the user is also logged out of Facebook.
3. A person logs into another webpage and into Facebook as part of the other webpage's login flow, then logs into your webpage. Upon logging out from either webpage, the person is logged out of Facebook.

Additionally, logging out of your webpage does not revoking permissions the person granted your webpage during login. [Revoking permissions](https://developers.facebook.com/docs/facebook-login/permissions#revokelogin) must be done separately. Build your webpage in such a way that a person who has logged out will not see the Login dialog when they log back in.

## Full Code Example {#example}

This code loads and initializes the JavaScript SDK in your HTML page. Replace `{app-id}` with your [app ID](https://developers.facebook.com/docs/apps) and `{api-version}` with the Graph API version to use. Unless you have a specific reason to use an older version, specify the most recent version: v25.0.

```
<!DOCTYPE html>
<html>
<head>
<title>Facebook Login JavaScript Example</title>
<meta charset="UTF-8">
</head>
<body>
<script>

  function statusChangeCallback(response) {  // Called with the results from FB.getLoginStatus().
    console.log('statusChangeCallback');
    console.log(response);                   // The current login status of the person.
    if (response.status === 'connected') {   // Logged into your webpage and Facebook.
      testAPI();
    } else {                                 // Not logged into your webpage or we are unable to tell.
      document.getElementById('status').innerHTML = 'Please log ' +
        'into this webpage.';
    }
  }

  function checkLoginState() {               // Called when a person is finished with the Login Button.
    FB.getLoginStatus(function(response) {   // See the onlogin handler
      statusChangeCallback(response);
    });
  }

  window.fbAsyncInit = function() {
    FB.init({
      appId      : '{app-id}',
      cookie     : true,                     // Enable cookies to allow the server to access the session.
      xfbml      : true,                     // Parse social plugins on this webpage.
      version    : '{api-version}'           // Use this Graph API version for this call.
    });

    FB.getLoginStatus(function(response) {   // Called after the JS SDK has been initialized.
      statusChangeCallback(response);        // Returns the login status.
    });
  };

  function testAPI() {                      // Testing Graph API after login.  See statusChangeCallback() for when this call is made.
    console.log('Welcome!  Fetching your information.... ');
    FB.api('/me', function(response) {
      console.log('Successful login for: ' + response.name);
      document.getElementById('status').innerHTML =
        'Thanks for logging in, ' + response.name + '!';
    });
  }

</script>

<!-- The JS SDK Login Button -->

<fb:login-button scope="public_profile,email" onlogin="checkLoginState();">
</fb:login-button>

<div id="status">
</div>

<!-- Load the JS SDK asynchronously -->
<script async defer crossorigin="anonymous" src="https://connect.facebook.net/en_US/sdk.js"></script>
</body>
</html>
```

### Additional Resources {#additional-resources}

* [Login Button](https://developers.facebook.com/documentation/facebook-login/web/login-button) Documentation
* [`FB.login()` Reference](https://developers.facebook.com/docs/reference/javascript/FB.login)
* [`FB.getLoginStatus()` Reference](https://developers.facebook.com/docs/reference/javascript/FB.getLoginStatus)
* [JavaScript SDK Reference](https://developers.facebook.com/docs/reference/javascript)
