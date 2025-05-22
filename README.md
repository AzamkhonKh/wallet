# Wallet-Net API

This is the backend API for the Wallet app, built with C# and .NET.

## Features

* User authentication and authorization
* Receipt scanning and OCR
* Transaction management
* Email notifications

## Technologies Used

This project is built with a modern technology stack:

*   **Backend Framework:** ASP.NET Core
*   **Primary Language:** C#
*   **Database:** PostgreSQL
*   **Object-Relational Mapper (ORM):** Entity Framework Core
*   **Authentication & Authorization:**
    *   ASP.NET Core Identity (for user management)
    *   JSON Web Tokens (JWT) (for stateless authentication)
*   **API Documentation:** Swagger / OpenAPI (integrated for exploring and testing endpoints)
*   **Containerization:** Docker & Docker Compose (for consistent development and deployment environments)
*   **Optical Character Recognition (OCR):** Tesseract OCR (for text extraction from images)
*   **Image Processing:**
    *   OpenCvSharp (for advanced image manipulation, used in receipt processing)
    *   SkiaSharp (available for cross-platform 2D graphics, minor use noted in OCR controller)
*   **Machine Learning (Receipt Analysis):** ONNX Runtime (for running the YOLO model used in receipt field detection)
*   **Email Services:** SMTP (via configured client for sending emails like OTP)

## Getting Started

### Prerequisites

* .NET SDK
* Docker (optional)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/wallet-net.git # Replace with the actual repository URL
   ```
2. Navigate to the project directory:
   ```bash
   cd wallet-net
   ```
3. **Database Setup:**
   - This project uses PostgreSQL as its database.
   - Ensure you have PostgreSQL installed and running.
   - Update the connection string in `appsettings.json` (and `appsettings.Development.json` if needed) with your PostgreSQL credentials:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=your_db_name;Username=your_username;Password=your_password"
     }
     ```
   - The application will automatically apply database migrations upon startup.

4. Restore .NET dependencies:
   ```bash
   dotnet restore
   ```

### Running the Application

#### Using .NET CLI

1.  **Ensure your PostgreSQL server is running and the connection string in `appsettings.json` is correctly configured.**
2.  Run the application:
    ```bash
    dotnet run
    ```
    The API will be available at `https://localhost:7000` or `http://localhost:5000` (check your console output). Database migrations will be applied automatically on startup.

#### Using Docker Compose (Recommended for Docker users)

This is the easiest way to get started with Docker as it sets up both the application and a PostgreSQL database container.

1.  **Ensure Docker and Docker Compose are installed.**
2.  Navigate to the project root directory (where `docker-compose.yaml` is located).
3.  Run the following command:
    ```bash
    docker-compose up -d
    ```
    This command will:
    * Build the `wallet-net` application image (if not already built).
    * Create and run a PostgreSQL container. The data will be persisted in a Docker volume.
    * Create and run the `wallet-net` application container, linking it to the PostgreSQL container.
4.  The API will be available at `http://localhost:5000` (or the port specified in `docker-compose.yaml`).

    To stop the services:
    ```bash
    docker-compose down
    ```

#### Using Docker (Manual Build and Run)

This method requires you to manage the database separately.

1.  **Ensure Docker is installed.**
2.  **Set up a PostgreSQL database accessible to Docker.** You can run PostgreSQL in a Docker container or use an existing instance. Make sure to update the `ConnectionStrings:DefaultConnection` in `appsettings.json` to point to this database instance *before* building the image, or manage this configuration using environment variables when running the container.
3.  Build the Docker image:
    ```bash
    docker build -t wallet-net .
    ```
4.  Run the Docker container:
    ```bash
    docker run -p 5000:5000 \
           -e ConnectionStrings__DefaultConnection="Host=<your_postgres_host>;Port=5432;Database=<your_db>;Username=<your_user>;Password=<your_pass>" \
           wallet-net
    ```
    Replace `<your_postgres_host>`, `<your_db>`, `<your_user>`, and `<your_pass>` with your actual database details. The API will be available at `http://localhost:5000`.

## API Endpoints

This section details the available API endpoints.

### General Notes:
*   **Base URL:** All API routes are prefixed with `/api`.
*   **Authentication:** Many endpoints require a JWT Bearer token in the `Authorization` header. These are marked with `🔒`.
*   **Error Responses:**
    *   `400 Bad Request`: Typically indicates invalid input, missing parameters, or failed validation. The response body often contains details about the errors.
    *   `401 Unauthorized`: Authentication is required, and it has failed or has not been provided.
    *   `403 Forbidden`: Authentication was successful, but the authenticated user does not have permission to access the resource.
    *   `404 Not Found`: The requested resource does not exist.
    *   `500 Internal Server Error`: An unexpected error occurred on the server.

---

### Auth Controller
**Base Route:** `/api/auth`

These endpoints handle user registration, login, token management, and OTP verification.

1.  **`POST /api/auth/register`**
    *   **Description:** Registers a new user.
    *   **Request Body:**
        ```json
        {
          "firstName": "John",
          "lastName": "Doe",
          "email": "user@example.com",
          "password": "Password123!",
          "confirmPassword": "Password123!"
        }
        ```
        *(Based on `RegisterDTO`)*
    *   **Response (200 OK):**
        ```json
        {
          "isSuccess": true,
          "message": "User registered successfully. Please check your email for OTP.",
          "token": "your_jwt_token", // May be null if OTP is required first
          "refreshToken": "your_refresh_token", // May be null
          "requiresOtp": true // Or false if auto-verified
        }
        ```
        *(Based on `AuthResponseDTO`)*
    *   **Response (400 Bad Request):** If validation fails or user already exists. Body contains error details.

2.  **`POST /api/auth/login`**
    *   **Description:** Logs in an existing user.
    *   **Request Body:**
        ```json
        {
          "email": "user@example.com",
          "password": "Password123!"
        }
        ```
        *(Based on `LoginDTO`)*
    *   **Response (200 OK):**
        ```json
        {
          "isSuccess": true,
          "message": "Login successful.",
          "token": "your_jwt_token",
          "refreshToken": "your_refresh_token",
          "requiresOtp": false // Or true if login triggers OTP for unverified accounts
        }
        ```
    *   **Response (400 Bad Request):** Invalid credentials or other login failure.

3.  **`POST /api/auth/verify-otp`**
    *   **Description:** Verifies the OTP sent to the user's email for actions like registration or login confirmation.
    *   **Request Body:**
        ```json
        {
          "email": "user@example.com",
          "otpCode": "123456"
        }
        ```
        *(Based on `VerifyOtpDTO`)*
    *   **Response (200 OK):** Similar to login, provides tokens upon successful OTP verification.
        ```json
        {
          "isSuccess": true,
          "message": "OTP verified successfully. Login complete.",
          "token": "your_jwt_token",
          "refreshToken": "your_refresh_token",
          "requiresOtp": false
        }
        ```
    *   **Response (400 Bad Request):** Invalid OTP or other failure.

4.  **`POST /api/auth/google-login`**
    *   **Description:** Authenticates a user using a Google ID token.
    *   **Request Body:**
        ```json
        {
          "provider": "Google", // Or other provider if extended
          "idToken": "google_id_token_string"
        }
        ```
        *(Based on `ExternalAuthDTO`)*
    *   **Response (200 OK):** Similar to login, provides tokens.
    *   **Response (400 Bad Request):** Invalid token or other failure.

5.  **`POST /api/auth/refresh-token`**
    *   **Description:** Refreshes an expired JWT access token using a valid refresh token.
    *   **Request Body:**
        ```json
        {
          "refreshToken": "your_existing_refresh_token"
        }
        ```
        *(Based on `RefreshTokenDTO`)*
    *   **Response (200 OK):** Provides a new set of tokens.
        ```json
        {
          "isSuccess": true,
          "message": "Token refreshed successfully.",
          "token": "new_jwt_token",
          "refreshToken": "new_refresh_token", // Can be the same or a new one
          "requiresOtp": false
        }
        ```
    *   **Response (400 Bad Request):** Invalid or expired refresh token.

6.  **`POST /api/auth/revoke-token`** 🔒
    *   **Description:** Revokes a specific refresh token for the authenticated user.
    *   **Authorization:** Requires JWT Bearer token.
    *   **Request Body:**
        ```json
        {
          "refreshToken": "token_to_revoke"
        }
        ```
        *(Based on `RefreshTokenDTO`)*
    *   **Response (200 OK):**
        ```json
        {
          "message": "Token revoked"
        }
        ```
    *   **Response (400 Bad Request):** If the token is missing or invalid.

7.  **`POST /api/auth/resend-otp`**
    *   **Description:** Resends an OTP to the user's email.
    *   **Request Body:**
        ```json
        {
          "email": "user@example.com"
        }
        ```
        *(Based on `EmailDTO` from `AuthController.cs`)*
    *   **Response (200 OK):**
        ```json
        {
          "message": "OTP sent successfully"
        }
        ```
    *   **Response (400 Bad Request):** If email is invalid or OTP sending fails.

---

### OCR Controller
**Base Route:** `/api/ocr`

Handles Optical Character Recognition (OCR) and image processing tasks.

1.  **`POST /api/ocr`**
    *   **Description:** Extracts text from an uploaded image file.
    *   **Request:** `IFormFile image` (sent as multipart/form-data).
    *   **Response (200 OK):**
        ```json
        {
          "text": "Extracted text from the image."
        }
        ```
    *   **Response (400 Bad Request):** "No file uploaded."
    *   **Response (500 Internal Server Error):** If OCR processing fails.

2.  **`POST /api/ocr/crop`**
    *   **Description:** Attempts to crop a receipt from an uploaded image.
        *   *Note: The backend implementation for cropping might be incomplete (`_receiptCropService.CropReceipt` was commented out in the source).*
    *   **Request:** `IFormFile image` (sent as multipart/form-data).
    *   **Response (200 OK):**
        ```json
        {
          "message": "Receipt cropped successfully.",
          "outputPath": "path/to/cropped_image.ext", // Server-side path
          "success": false // Indicates current implementation status
        }
        ```
    *   **Response (400 Bad Request):** "No file uploaded."

---

### Receipt Controller
**Base Route:** `/api/receipt` (Assumed, as controller is `ReceiptController` and actions don't have full route)

Processes uploaded receipt images to identify and extract structured data using a YOLO model.

1.  **`POST /api/receipt/process`** (Route assumed, could be just `/process`)
    *   **Description:** Processes a receipt image, detects fields (e.g., Date, TotalPrice), crops them, and returns paths to these cropped images.
    *   **Request:** `IFormFile file` (sent as multipart/form-data) - The receipt image.
    *   **Response (200 OK):** A dictionary mapping detected class names to relative server paths of the cropped images.
        ```json
        {
          "Title": "unique_receipt_id/Title_0.jpg",
          "TotalPrice": "unique_receipt_id/TotalPrice_1.jpg",
          "Date": "unique_receipt_id/Date_2.jpg"
          // ... other detected fields
        }
        ```
    *   **Response (400 Bad Request):** "No file uploaded."
    *   **Response (500 Internal Server Error):** If image processing or model inference fails.

---

### Transaction Controller
**Base Route:** `/api/transaction`

Manages financial transactions.
*Note: The `Transaction` model in this project is very basic (`Id` (int), `Name` (string)). The endpoints reflect this simplicity.*

1.  **`GET /api/transaction`**
    *   **Description:** Retrieves a list of all transactions.
    *   **Authorization:** Allowed for anonymous users.
    *   **Response (200 OK):** An array of `Transaction` objects.
        ```json
        [
          { "id": 1, "name": "Lunch at Cafe" },
          { "id": 2, "name": "Salary Deposit" }
        ]
        ```

2.  **`GET /api/transaction/{id}`** 🔒
    *   **Description:** Retrieves a specific transaction by its ID.
    *   **Authorization:** Requires JWT Bearer token.
    *   **Path Parameter:** `id` (long) - The ID of the transaction.
    *   **Response (200 OK):** The `Transaction` object.
        ```json
        { "id": 1, "name": "Lunch at Cafe" }
        ```
    *   **Response (404 Not Found):** If the transaction does not exist.

3.  **`POST /api/transaction`** 🔒
    *   **Description:** Creates a new transaction.
    *   **Authorization:** Requires JWT Bearer token.
    *   **Request Body:** `Transaction` object.
        ```json
        {
          "name": "Groceries"
          // "id" is auto-generated
        }
        ```
    *   **Response (201 Created):** The created `Transaction` object with its new `id`.
        ```json
        { "id": 3, "name": "Groceries" }
        ```
        Includes a `Location` header to the new resource.

4.  **`PUT /api/transaction/{id}`** 🔒
    *   **Description:** Updates an existing transaction.
    *   **Authorization:** Requires JWT Bearer token.
    *   **Path Parameter:** `id` (long) - The ID of the transaction to update.
    *   **Request Body:** `Transaction` object (must include `id` matching the path).
        ```json
        {
          "id": 1, // Must match {id} in path
          "name": "Dinner at Restaurant"
        }
        ```
    *   **Response (204 No Content):** On successful update.
    *   **Response (400 Bad Request):** If `id` in body doesn't match `id` in path.
    *   **Response (404 Not Found):** If the transaction does not exist.

5.  **`DELETE /api/transaction/{id}`** 🔒
    *   **Description:** Deletes a transaction by its ID.
    *   **Authorization:** Requires JWT Bearer token.
    *   **Path Parameter:** `id` (long) - The ID of the transaction to delete.
    *   **Response (204 No Content):** On successful deletion.
    *   **Response (404 Not Found):** If the transaction does not exist.

---

### User Controller
**Base Route:** `/api/user`

Manages user accounts.
*Security Warning: The endpoints in this controller currently lack `[Authorize]` attributes. This is a significant security risk as it would allow unauthenticated access to user data and management functions. This should be addressed by adding appropriate authorization.*

1.  **`GET /api/user`**
    *   **Description:** Retrieves a list of all users.
    *   **Authorization:** None (Potential security risk - exposes all user data).
    *   **Response (200 OK):** An array of `User` objects.
        *   Each `User` object includes: `id` (int), `firstName`, `lastName`, `email`, `userName`, `createdAt`, `isEmailVerified`, and other properties inherited from `IdentityUser`.
        *   *Note: Exposing the full `User` model, especially fields like `otpCode`, `passwordHash` (from IdentityUser), is a security concern. A `UserDTO` should be used for responses.*
        ```json
        [
          { "id": 1, "firstName": "Admin", "lastName": "User", "email": "admin@example.com", ... },
          { "id": 2, "firstName": "Jane", "lastName": "Doe", "email": "jane@example.com", ... }
        ]
        ```

2.  **`GET /api/user/{id}`**
    *   **Description:** Retrieves a specific user by their `int` ID.
    *   **Authorization:** None (Potential security risk).
    *   **Path Parameter:** `id` (int) - The integer ID of the user.
    *   **Response (200 OK):** The `User` object (see structure above).
    *   **Response (404 Not Found):** If the user does not exist.

3.  **`PUT /api/user/{id}`**
    *   **Description:** Updates an existing user.
    *   **Authorization:** None (Potential security risk - allows unauthenticated updates).
    *   **Path Parameter:** `id` (int) - The integer ID of the user to update.
    *   **Request Body:** `User` object.
        *   *Note: Client should send only fields intended for update. Backend should carefully control which fields can be updated.*
        ```json
        {
          "id": 2, // Must match {id} in path
          "firstName": "Janet",
          // ... other updatable fields
        }
        ```
    *   **Response (204 No Content):** On successful update.
    *   **Response (400 Bad Request):** If `id` in body doesn't match `id` in path.
    *   **Response (404 Not Found):** If the user does not exist.

4.  **`DELETE /api/user/{id}`**
    *   **Description:** Deletes a user by their `int` ID.
    *   **Authorization:** None (Potential security risk - allows unauthenticated deletions).
    *   **Path Parameter:** `id` (int) - The integer ID of the user to delete.
    *   **Response (204 No Content):** On successful deletion.
    *   **Response (404 Not Found):** If the user does not exist.

## Contributing

We welcome contributions to the Wallet-Net API! Whether you're fixing a bug, adding a new feature, or improving documentation, your help is appreciated.

### How to Contribute

1.  **Reporting Bugs or Requesting Features:**
    *   Please open an issue on the GitHub repository.
    *   For bugs, provide as much detail as possible, including steps to reproduce, expected behavior, and actual behavior.
    *   For feature requests, clearly describe the proposed functionality and its potential benefits.

2.  **Contributing Code:**
    *   **Fork the Repository:** Start by forking the main repository to your own GitHub account.
    *   **Create a Branch:** Create a new branch in your forked repository for your feature or bug fix. Use a descriptive name (e.g., `feat/add-new-endpoint` or `fix/user-auth-bug`).
        ```bash
        git checkout -b feat/your-feature-name
        ```
    *   **Make Changes:** Write your code, ensuring you follow any existing coding styles and conventions.
        *   *(Project maintainers: Consider adding a `CONTRIBUTING.md` file with detailed coding standards, or link to style guides here.)*
    *   **Test Your Changes:**
        *   Ensure your changes don't break existing functionality.
        *   If applicable, add unit tests or integration tests for your new code.
        *   *(Project maintainers: Provide instructions on how to run tests, e.g., `dotnet test`.)*
    *   **Commit Your Changes:** Write clear and concise commit messages.
        ```bash
        git commit -m "feat: Implement user profile update endpoint"
        ```
    *   **Push to Your Fork:** Push your changes to your forked repository.
        ```bash
        git push origin feat/your-feature-name
        ```
    *   **Submit a Pull Request (PR):** Open a pull request from your branch to the `main` (or `develop`) branch of the original Wallet-Net repository.
        *   Provide a clear title and description for your PR, explaining the changes you've made and why.
        *   Link to any relevant issues.

3.  **Pull Request Review:**
    *   Maintainers will review your PR.
    *   Be prepared to discuss your changes and make adjustments based on feedback.
    *   Once approved, your PR will be merged.

Thank you for contributing to Wallet-Net!

## License

This project is licensed under the MIT License.
