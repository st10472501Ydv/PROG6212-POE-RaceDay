# RaceDay - API Endpoint Plan

This document lists all the API endpoints that the RaceDay system will expose. The API is built using C# with ASP.NET Core in Part 2 of this project.

## Overview

The API has 19 endpoints across 6 categories:
- Authentication (2 endpoints) - register and login
- User Profile (2 endpoints) - view and update profile
- Events (5 endpoints) - CRUD operations for events
- Categories (4 endpoints) - manage event categories
- Enrolments (3 endpoints) - sign up for events
- Results (3 endpoints) - capture and view race results

## Authentication Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | /api/auth/register | Creates a new user account. The user picks whether they are an Organiser or Participant during signup. | None (public) | `{ "fullName": "string", "email": "string", "password": "string", "role": "Organiser or Participant" }` | 201 Created - returns the new user ID and a success message. 400 Bad Request - if email is already taken or fields are missing. |
| POST | /api/auth/login | Signs the user in and returns a JWT token they use for authenticated requests. | None (public) | `{ "email": "string", "password": "string" }` | 200 OK - returns a JWT token and user info. 401 Unauthorized - if email or password is wrong. |

## User Profile Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/users/profile | Gets the profile of the user who is currently logged in. | Any (logged in) | None | 200 OK - returns the user's name, email, role, and date joined. 401 Unauthorized - if no valid token. |
| PUT | /api/users/profile | Lets the user update their name and email. | Any (logged in) | `{ "fullName": "string", "email": "string" }` | 200 OK - returns updated profile. 400 Bad Request - if the new email is already taken. 401 Unauthorized - if not logged in. |

## Events Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events | Returns a list of all active events. Anyone can see what events are coming up. | None (public) | None | 200 OK - returns an array of events with their basic info (name, date, location). |
| POST | /api/events | Creates a new event. Only organisers can do this, and the event gets linked to their account. | Organiser | `{ "eventName": "string", "description": "string", "eventDate": "datetime", "location": "string" }` | 201 Created - returns the new event with its ID. 400 Bad Request - if required fields are missing. 403 Forbidden - if a Participant tries to create an event. |
| GET | /api/events/{id} | Gets the full details of one event, including all its categories. | None (public) | None | 200 OK - returns event details with categories array. 404 Not Found - if the event does not exist. |
| PUT | /api/events/{id} | Updates an event's details. Only the organiser who created it can do this. | Organiser | `{ "eventName": "string", "description": "string", "eventDate": "datetime", "location": "string" }` | 200 OK - returns the updated event. 403 Forbidden - if the user is not the organiser who created the event. 404 Not Found - if the event does not exist. |
| DELETE | /api/events/{id} | Deletes an event. This also removes all its categories and enrolments. Only the organiser who created it can do this. | Organiser | None | 204 No Content - if deleted successfully. 403 Forbidden - if the user is not the owner. 404 Not Found - if the event does not exist. |

## Categories Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events/{eventId}/categories | Lists all categories for a specific event. | None (public) | None | 200 OK - returns an array of categories with name, max participants, and entry fee. 404 Not Found - if the event does not exist. |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. Only the event's organiser can do this. | Organiser | `{ "categoryName": "string", "maxParticipants": 0, "entryFee": 0.00 }` | 201 Created - returns the new category. 400 Bad Request - if fields are missing. 403 Forbidden - if the user is not the event's organiser. 404 Not Found - if the event does not exist. |
| PUT | /api/categories/{id} | Updates a category's details. Only the organiser who owns the parent event can do this. | Organiser | `{ "categoryName": "string", "maxParticipants": 0, "entryFee": 0.00 }` | 200 OK - returns the updated category. 403 Forbidden - if not the owner. 404 Not Found - if the category does not exist. |
| DELETE | /api/categories/{id} | Removes a category from an event. This will also remove any enrolments in that category. | Organiser | None | 204 No Content - if deleted. 403 Forbidden - if not the owner. 404 Not Found - if not found. |

## Enrolments Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| POST | /api/categories/{categoryId}/enrol | Lets a participant sign up for a category. The system checks if the category is full before allowing it. | Participant | None | 201 Created - returns the enrolment with a confirmation message. 409 Conflict - if the participant is already enrolled or the category is full. 403 Forbidden - if an Organiser tries to enrol. 404 Not Found - if the category does not exist. |
| GET | /api/users/enrolments | Shows all the events a participant has signed up for. | Participant | None | 200 OK - returns an array of enrolments with event and category details. 401 Unauthorized - if not logged in. |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. The participant can only cancel their own enrolments. | Participant | None | 204 No Content - if cancelled. 403 Forbidden - if trying to cancel someone else's enrolment. 404 Not Found - if the enrolment does not exist. |

## Results Endpoints

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|-------------|-------|-------------|---------------|--------------|-------------------|
| GET | /api/events/{eventId}/results | Shows the results for an event, sorted by position. | None (public) | None | 200 OK - returns results with participant name, finish time, and position. 404 Not Found - if the event does not exist. |
| POST | /api/events/{eventId}/results | Adds a result for a participant. Only the event's organiser can do this. | Organiser | `{ "enrolmentId": 0, "finishTime": "HH:MM:SS", "position": 0, "status": "Finished" }` | 201 Created - returns the result. 400 Bad Request - if the enrolment ID is invalid. 403 Forbidden - if not the event's organiser. 409 Conflict - if a result already exists for this enrolment. 404 Not Found - if the event does not exist. |
| PUT | /api/results/{id} | Updates a result. Only the event's organiser can do this. | Organiser | `{ "finishTime": "HH:MM:SS", "position": 0, "status": "Finished" }` | 200 OK - returns the updated result. 403 Forbidden - if not the organiser. 404 Not Found - if the result does not exist. |

## Notes on the API Design

- All routes start with `/api/` to keep things consistent.
- The system uses JWT tokens for authentication. The token is passed in the `Authorization` header as `Bearer <token>`.
- Role-based access is checked at the API level. If a Participant tries to access an Organiser-only endpoint, they get a 403 Forbidden response.
- I kept the routes pretty simple and readable. The nesting makes it clear which event a category or result belongs to.
- The DELETE endpoints return 204 No Content instead of 200 because there is nothing to send back.
- Error responses include a message that explains what went wrong so the front-end can show it to the user.

## Endpoint Count Summary

| Category | Number of Endpoints |
|----------|-------------------|
| Authentication | 2 |
| User Profile | 2 |
| Events | 5 |
| Categories | 4 |
| Enrolments | 3 |
| Results | 3 |
| **Total** | **19** |

---

*Note: AI tools were used to assist with planning and drafting this document.*
