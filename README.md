# BookStore API

## 📌 Overview

The **BookStore API** is a RESTful API built with **ASP.NET Core** that allows users to manage books and authentication using **JWT-based authentication with refresh tokens**. This API supports CRUD operations for books and user authentication with **access and refresh tokens** stored in HTTP-only cookies.

## 🚀 Features

- **User Authentication & Authorization** (JWT + Refresh Tokens in HTTP-only cookies)
- **User Registration & Login**
- **Book Management** (Create, Read, Update, Delete)
- **Token Rotation for Security**
- **Logout & Token Revocation**
- **Entity Framework Core with SQL Server**

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core Web API
- **Database**: SQL Server
- **ORM**: Entity Framework Core
- **Authentication**: JWT & Refresh Tokens
- **Security**: HTTP-only Cookies for Refresh Tokens

## 🔧 Setup & Installation

### **1️⃣ Clone the Repository**

```sh
git clone https://github.com/yourusername/BookStoreApi.git
cd BookStoreApi
```

### **2️⃣ Configure the Database**

Modify `appsettings.json` to set up your SQL Server connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=BookStoreDB;Trusted_Connection=True;"
}
```

Run database migrations:

```sh
dotnet ef database update
```

### **3️⃣ Run the Application**

```sh
dotnet run
```

The API will be available at `http://localhost:5000` (or a different port if configured).

## 🔑 Authentication Flow

### **User Registration**

- `POST /api/auth/register`
- Request Body:

```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePass123"
}
```

### **User Login (Generates Access & Refresh Tokens)**

- `POST /api/auth/login`
- Request Body:

```json
{
  "username": "john_doe",
  "password": "SecurePass123"
}
```

- Response:

```json
{
  "accessToken": "JWT_ACCESS_TOKEN"
}
```

- Refresh token is stored in an **HTTP-only cookie**.

### **Refreshing Tokens**

- `POST /api/auth/refresh`
- Automatically refreshes an expired access token using the **refresh token stored in cookies**.

### **Logout & Token Revocation**

- `POST /api/auth/logout`
- Deletes the refresh token from the database and removes the cookie.

## 📚 Book API Endpoints

| Method | Endpoint          | Description         | Auth Required |
| ------ | ----------------- | ------------------- | ------------- |
| GET    | `/api/books`      | Get all books       | ✅ Yes       |
| GET    | `/api/books/{id}` | Get book by ID      | ✅ Yes       | 
| POST   | `/api/books`      | Create a new book   | ✅ Yes       |
| PUT    | `/api/books/{id}` | Update book details | ✅ Yes       |
| DELETE | `/api/books/{id}` | Delete a book       | ✅ Yes       |

## 🔒 Security Best Practices Implemented

- **JWT with HTTP-only Refresh Tokens** (Prevents XSS attacks)
- **Token Rotation** (Old refresh tokens are revoked after use)
- **Hashed Password Storage** (BCrypt)




