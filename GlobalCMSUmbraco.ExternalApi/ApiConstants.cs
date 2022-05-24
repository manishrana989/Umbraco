
namespace GlobalCMSUmbraco.ExternalApi
{
    public static class ApiConstants
    {
        public const string SecretKey = "a84e6e5e927048e3abc69506adb4ab7298bb71982140485da2f7e77af9c61f48";

        public const int TokenExpiresMinutes = 120;

        // API should not be using 'demo' starter kits
        public const string StarterKitDoesNotContain = "demo";

        public const string SwaggerTitle = "GlobalCMS External API";

        public static string SwaggerDescription { get; } = $@"
## Authentication

You must pass a Bearer Token with all endpoint requests.
Include the following header in your request making sure to replace `$BEARER_TOKEN` with your generated Bearer Token:

`'Authorization: Bearer $BEARER_TOKEN'`

You should generate a Bearer Token by using the **TokenApi:Generate** endpoint specified below and the API key you should have been provided for the target environment.

Once you have generated a token if you want to test endpoints using this tool, enter `Bearer $BEARER_TOKEN` in the 'api_key' textbox at the top of this page and press 'Explore'. 
You can use the **TokenApi:Confirm** endpoint to confirm that your bearer token is authenticated.

Please note that tokens will expire after {TokenExpiresMinutes} minutes. 

## Projects

Use the **ProjectsApi** endpoints to perform all project 'CRUD' operations. 

Projects are managed separately in each environment. Please ensure that you are targeting the correct environment when working with this API.
Publishing projects from one environment to another is not currently supported via the API. The process to follow in that scenario is to be defined.

All endpoints will return a `401` status code if the bearer token is missing / unauthorised.

### Adding New Projects

There are several steps involved in adding a new project. Each step has its own API endpoint, and should be called in the following order:

- `CreateProject` : adds the project to the Projects list in the targeted environment and creates its MongoDb collections
- `ConfigureRealm` : creates the Realm Apps for the project
- `CreateRealmUser` : creates a MongoDb Realm user, will need to be called twice (once for preview db, once for publish db)
- `CloneStarterKit` : creates Content and Media in the Umbraco backoffice for the project from the chosen starter kit
- `ConfigurePermissions` : creates a new User group for the project, and adds the project admin user (and emails login details)

### Updating Projects

The `GetOverview` endpoint returns the key information about a project.

The `GetSettings` endpoint returns all the editable settings for a project with its current values.

The `UpdateSettings` endpoint will save the updated project settings.

### Deleting Projects

Projects can only be deleted via the API if no content or media has been added for the project. 

The `GetOverview` endpoint returns all the project information, including a flag for whether it can be deleted.

The `DeleteProject` endpoint will delete the project (if allowed to do so).
";
    }
}
