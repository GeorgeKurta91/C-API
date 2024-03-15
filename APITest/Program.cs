using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private static string token = "";

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please specify which test to run (login, addcontact, wronglogin, wrongdataformat, gettest, brokenapi, updatecontact, deletecontact, concurrency, servererror, networktimeout).");
            return;
        }

        switch (args[0].ToLower())
        {
            case "login":
                await TestLogin();
                break;
            case "addcontact":
                await EnsureLoggedInAndTest(TestAddContact);
                break;
            case "wronglogin":
                await TestWrongLogin();
                break;
            case "wrongdataformat":
                await EnsureLoggedInAndTest(TestWrongDataFormat);
                break;
            case "gettest":
                await EnsureLoggedInAndTest(TestGetContact);
                break;
            case "brokenapi":
                await EnsureLoggedInAndTest(TestBrokenAPI);
                break;
            case "updatecontact":
                await EnsureLoggedInAndTest(TestAddOrUpdateContact);
                break;
            case "deletecontact":
                await EnsureLoggedInAndTest(TestDeleteContact);
                break;
            case "concurrency":
                await TestConcurrency();
                break;
            case "servererror":
                await TestServerError();
                break;
            case "networktimeout":
                await TestNetworkTimeout();
                break;
            default:
                Console.WriteLine("Unknown test. Please specify a valid test name.");
                break;
        }
    }

    // Method to ensure user is logged in before executing a test
    static async Task EnsureLoggedInAndTest(Func<Task> testFunction)
    {
        if (string.IsNullOrEmpty(token))
        {
            await TestLogin();
        }
        await testFunction();
    }

    // Method to perform login test
    static async Task TestLogin()
    {
        // Login credentials
        var loginData = new { email = "phytontest@test.com", password = "Testpass" };

        // Send POST request to login endpoint
        var loginResponse = await httpClient.PostAsJsonAsync("https://thinking-tester-contact-list.herokuapp.com/users/login", loginData);
        
        if (loginResponse.IsSuccessStatusCode)
        {
            Console.WriteLine("Login successful.");
            var responseBody = await loginResponse.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            // Extract token from response and set it for future requests
            if (responseJson.TryGetProperty("token", out var tokenElement))
            {
                token = tokenElement.GetString();
                httpClient.DefaultRequestHeaders.Authorization = new("Bearer", token);
            }
            else
            {
                Console.WriteLine("Token not found in the response.");
            }
        }
        else
        {
            Console.WriteLine($"Login failed with status code: {loginResponse.StatusCode}");
        }
    }

    // Method to add a new contact
    static async Task TestAddContact()
    {
        // Contact data
        var contactData = new
        {
            firstName = "George",
            lastName = "Test",
            birthdate = "1970-01-01",
            email = "jdoe@fake.com",
            phone = "8005555555",
            street1 = "1 Main St.",
            street2 = "Apartment A",
            city = "Anytown",
            stateProvince = "KS",
            postalCode = "12345",
            country = "USA"
        };

        // Send POST request to add contact endpoint
        var addContactResponse = await httpClient.PostAsJsonAsync("https://thinking-tester-contact-list.herokuapp.com/contacts", contactData);

        // Check response status
        if (addContactResponse.IsSuccessStatusCode)
        {
            Console.WriteLine("Contact added successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to add contact with status code: {addContactResponse.StatusCode}");
        }
    }

    // Method to handle incorrect login test
    static async Task TestWrongLogin()
    {
        // Incorrect login credentials
        var wrongLoginData = new { email = "wrongemail@test.com", password = "WrongPass" };

        // Send POST request with incorrect credentials
        var loginResponse = await httpClient.PostAsJsonAsync("https://thinking-tester-contact-list.herokuapp.com/users/login", wrongLoginData);

        // Check if response status is Unauthorized (401)
        Console.WriteLine(loginResponse.StatusCode == HttpStatusCode.Unauthorized
            ? "Login attempt unauthorized as expected."
            : $"Unexpected response status: {loginResponse.StatusCode}");
    }

    // Method to handle wrong data format test
    static async Task TestWrongDataFormat()
    {
        // Intentionally empty data (wrong format)
        var wrongFormatData = new { };

        // Send POST request with incorrect data format
        var response = await httpClient.PostAsJsonAsync("https://thinking-tester-contact-list.herokuapp.com/contacts", wrongFormatData);

        // Check if response status is BadRequest (400) or NotFound (404)
        Console.WriteLine((response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
            ? $"Received expected status code: {response.StatusCode}"
            : $"Unexpected response status: {response.StatusCode}");
    }

    // Method to retrieve contacts and search for 'George'
    static async Task TestGetContact()
    {
        // Send GET request to retrieve contacts
        var getContactsResponse = await httpClient.GetAsync("https://thinking-tester-contact-list.herokuapp.com/contacts");

        if (getContactsResponse.IsSuccessStatusCode)
        {
            // Read response body
            var responseBody = await getContactsResponse.Content.ReadAsStringAsync();

            // Check if 'George' is found in the response
            if (responseBody.Contains("George"))
            {
                Console.WriteLine("Contact 'George' found.");
            }
            else
            {
                Console.WriteLine("Contact 'George' not found.");
            }
        }
        else
        {
            Console.WriteLine($"Failed to retrieve contacts with status code: {getContactsResponse.StatusCode}");
        }
    }

    // Method to test broken API endpoint
    static async Task TestBrokenAPI()
    {
        // Intentionally incorrect data format
        var wrongData = new { };

        // Attempt to send request with incorrect data
        var response = await httpClient.PostAsJsonAsync("https://thinking-tester-contact-list.herokuapp.com/contacts", wrongData);

        // Check if response status code is BadRequest (400) or NotFound (404)
        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Received expected status code: {response.StatusCode}");
        }
        else
        {
            Console.WriteLine($"Unexpected response status: {response.StatusCode}");
        }
    }

    // Method to add or update a contact
    static async Task TestAddOrUpdateContact()
    {
        // Implement your logic here to test adding or updating a contact
        // You can use the same API endpoint for both operations
    }

    // Method to delete a contact
    static async Task TestDeleteContact()
    {
        // Implement your logic here to test deleting a contact
        // Use the appropriate API endpoint to delete a contact
    }

    // Method to test concurrency by sending multiple requests simultaneously
    static async Task TestConcurrency()
    {
        List<Task> tasks = new List<Task>();
        int concurrentRequests = 10; // Number of concurrent requests

        // Create tasks for concurrent requests
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(SendConcurrentRequest(i));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
    }

    // Helper method to send a single concurrent request
    static async Task SendConcurrentRequest(int requestId)
    {
        try
        {
            // Send GET request to the API endpoint
            var response = await httpClient.GetAsync("https://thinking-tester-contact-list.herokuapp.com/users");

            // Log response status code
            Console.WriteLine($"Request {requestId}: Response Status Code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            // Log any errors that occur during the request
            Console.WriteLine($"Request {requestId}: Error - {ex.Message}");
        }
    }

    // Method to test server error scenario
    static async Task TestServerError()
    {
        // Send GET request to an endpoint known to trigger a server error
        var response = await httpClient.GetAsync("https://thinking-tester-contact-list.herokuapp.com/error");

        // Check if response status code indicates a server error (500)
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            // Log the response body for further analysis
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Server Error Response Body: {responseBody}");
        }
        else
        {
            Console.WriteLine($"Unexpected Response Status Code: {response.StatusCode}");
        }
    }

    // Method to test network timeout scenario
    static async Task TestNetworkTimeout()
    {
        // Set a short timeout for the HTTP client to simulate network timeout
        httpClient.Timeout = TimeSpan.FromSeconds(1); // 1 second timeout

        try
        {
            // Send GET request to an endpoint known to cause network timeout
            var response = await httpClient.GetAsync("https://thinking-tester-contact-list.herokuapp.com/slow-endpoint");
            Console.WriteLine($"Response Status Code: {response.StatusCode}");
        }
        catch (TaskCanceledException)
        {
            // Handle network timeout exception
            Console.WriteLine("Network Timeout occurred.");
        }
    }
}
