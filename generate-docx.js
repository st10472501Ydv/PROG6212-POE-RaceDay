// generate-docx.js
// This script creates the Word document version of the API Endpoint Plan
// Run with: node generate-docx.js
// Requires: npm install docx

const { Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
        Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType, 
        ShadingType, PageNumber, PageBreak, FootnoteReferenceRun } = require('docx');
const fs = require('fs');

// Helper to create a table cell
function cell(text, width, isHeader = false) {
    const border = { style: BorderStyle.SINGLE, size: 1, color: "999999" };
    const borders = { top: border, bottom: border, left: border, right: border };
    
    return new TableCell({
        borders,
        width: { size: width, type: WidthType.DXA },
        shading: isHeader ? { fill: "2E5090", type: ShadingType.CLEAR } : undefined,
        margins: { top: 60, bottom: 60, left: 100, right: 100 },
        children: [new Paragraph({
            children: [new TextRun({
                text: text,
                bold: isHeader,
                color: isHeader ? "FFFFFF" : "000000",
                font: "Arial",
                size: isHeader ? 20 : 18
            })]
        })]
    });
}

// The endpoint data
const endpoints = [
    ["POST", "/api/auth/register", "Creates a new user account. The user picks whether they are an Organiser or Participant during signup.", "None (public)", '{ "fullName": "string", "email": "string", "password": "string", "role": "Organiser or Participant" }', "201 Created - returns the new user ID and a success message. 400 Bad Request - if email is already taken or fields are missing."],
    ["POST", "/api/auth/login", "Signs the user in and returns a JWT token they use for authenticated requests.", "None (public)", '{ "email": "string", "password": "string" }', "200 OK - returns a JWT token and user info. 401 Unauthorized - if email or password is wrong."],
    ["GET", "/api/users/profile", "Gets the profile of the user who is currently logged in.", "Any (logged in)", "None", "200 OK - returns the user's name, email, role, and date joined. 401 Unauthorized - if no valid token."],
    ["PUT", "/api/users/profile", "Lets the user update their name and email.", "Any (logged in)", '{ "fullName": "string", "email": "string" }', "200 OK - returns updated profile. 400 Bad Request - if the new email is already taken. 401 Unauthorized - if not logged in."],
    ["GET", "/api/events", "Returns a list of all active events. Anyone can see what events are coming up.", "None (public)", "None", "200 OK - returns an array of events with their basic info (name, date, location)."],
    ["POST", "/api/events", "Creates a new event. Only organisers can do this, and the event gets linked to their account.", "Organiser", '{ "eventName": "string", "description": "string", "eventDate": "datetime", "location": "string" }', "201 Created - returns the new event with its ID. 400 Bad Request - if required fields are missing. 403 Forbidden - if a Participant tries to create an event."],
    ["GET", "/api/events/{id}", "Gets the full details of one event, including all its categories.", "None (public)", "None", "200 OK - returns event details with categories array. 404 Not Found - if the event does not exist."],
    ["PUT", "/api/events/{id}", "Updates an event's details. Only the organiser who created it can do this.", "Organiser", '{ "eventName": "string", "description": "string", "eventDate": "datetime", "location": "string" }', "200 OK - returns the updated event. 403 Forbidden - if the user is not the organiser who created the event. 404 Not Found - if the event does not exist."],
    ["DELETE", "/api/events/{id}", "Deletes an event. This also removes all its categories and enrolments. Only the organiser who created it can do this.", "Organiser", "None", "204 No Content - if deleted successfully. 403 Forbidden - if the user is not the owner. 404 Not Found - if the event does not exist."],
    ["GET", "/api/events/{eventId}/categories", "Lists all categories for a specific event.", "None (public)", "None", "200 OK - returns an array of categories with name, max participants, and entry fee. 404 Not Found - if the event does not exist."],
    ["POST", "/api/events/{eventId}/categories", "Adds a new category to an event. Only the event's organiser can do this.", "Organiser", '{ "categoryName": "string", "maxParticipants": 0, "entryFee": 0.00 }', "201 Created - returns the new category. 400 Bad Request - if fields are missing. 403 Forbidden - if the user is not the event's organiser. 404 Not Found - if the event does not exist."],
    ["PUT", "/api/categories/{id}", "Updates a category's details. Only the organiser who owns the parent event can do this.", "Organiser", '{ "categoryName": "string", "maxParticipants": 0, "entryFee": 0.00 }', "200 OK - returns the updated category. 403 Forbidden - if not the owner. 404 Not Found - if the category does not exist."],
    ["DELETE", "/api/categories/{id}", "Removes a category from an event. This will also remove any enrolments in that category.", "Organiser", "None", "204 No Content - if deleted. 403 Forbidden - if not the owner. 404 Not Found - if not found."],
    ["POST", "/api/categories/{categoryId}/enrol", "Lets a participant sign up for a category. The system checks if the category is full before allowing it.", "Participant", "None", "201 Created - returns the enrolment with a confirmation message. 409 Conflict - if the participant is already enrolled or the category is full. 403 Forbidden - if an Organiser tries to enrol. 404 Not Found - if the category does not exist."],
    ["GET", "/api/users/enrolments", "Shows all the events a participant has signed up for.", "Participant", "None", "200 OK - returns an array of enrolments with event and category details. 401 Unauthorized - if not logged in."],
    ["DELETE", "/api/enrolments/{id}", "Cancels an enrolment. The participant can only cancel their own enrolments.", "Participant", "None", "204 No Content - if cancelled. 403 Forbidden - if trying to cancel someone else's enrolment. 404 Not Found - if the enrolment does not exist."],
    ["GET", "/api/events/{eventId}/results", "Shows the results for an event, sorted by position.", "None (public)", "None", "200 OK - returns results with participant name, finish time, and position. 404 Not Found - if the event does not exist."],
    ["POST", "/api/events/{eventId}/results", "Adds a result for a participant. Only the event's organiser can do this.", "Organiser", '{ "enrolmentId": 0, "finishTime": "HH:MM:SS", "position": 0, "status": "Finished" }', "201 Created - returns the result. 400 Bad Request - if the enrolment ID is invalid. 403 Forbidden - if not the event's organiser. 409 Conflict - if a result already exists for this enrolment. 404 Not Found - if the event does not exist."],
    ["PUT", "/api/results/{id}", "Updates a result. Only the event's organiser can do this.", "Organiser", '{ "finishTime": "HH:MM:SS", "position": 0, "status": "Finished" }', "200 OK - returns the updated result. 403 Forbidden - if not the organiser. 404 Not Found - if the result does not exist."]
];

// Column widths (total = 9360 for US Letter with 1" margins)
// Method: 900, Route: 1800, Desc: 2200, Role: 1100, Body: 1660, Response: 1700
const colWidths = [900, 1800, 2200, 1100, 1660, 1700];

// Build table rows
const headerRow = new TableRow({
    tableHeader: true,
    children: [
        cell("HTTP Method", colWidths[0], true),
        cell("Route", colWidths[1], true),
        cell("Description", colWidths[2], true),
        cell("Role Required", colWidths[3], true),
        cell("Request Body", colWidths[4], true),
        cell("Expected Response", colWidths[5], true)
    ]
});

const dataRows = endpoints.map(row => new TableRow({
    children: row.map((text, i) => cell(text, colWidths[i]))
}));

const doc = new Document({
    styles: {
        default: { document: { run: { font: "Arial", size: 24 } } },
        paragraphStyles: [
            { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
                run: { size: 36, bold: true, font: "Arial" },
                paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
            { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
                run: { size: 28, bold: true, font: "Arial" },
                paragraph: { spacing: { before: 240, after: 160 }, outlineLevel: 1 } },
        ]
    },
    footnotes: {
        1: { children: [new Paragraph("AI tools were used to assist with planning and drafting this document.")] }
    },
    sections: [{
        properties: {
            page: {
                size: { width: 12240, height: 15840 },
                margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 }
            }
        },
        headers: {
            default: new Header({
                children: [new Paragraph({
                    children: [new TextRun({ text: "RaceDay - API Endpoint Plan", font: "Arial", size: 18, color: "666666" })],
                    alignment: AlignmentType.RIGHT
                })]
            })
        },
        footers: {
            default: new Footer({
                children: [new Paragraph({
                    children: [
                        new TextRun({ text: "Page ", font: "Arial", size: 18 }),
                        new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 18 })
                    ],
                    alignment: AlignmentType.CENTER
                })]
            })
        },
        children: [
            // Title
            new Paragraph({
                heading: HeadingLevel.HEADING_1,
                children: [new TextRun({ text: "RaceDay - API Endpoint Plan", bold: true })]
            }),
            
            // Student info
            new Paragraph({
                spacing: { after: 200 },
                children: [
                    new TextRun({ text: "Student: ", bold: true }),
                    new TextRun("[Your Name] | "),
                    new TextRun({ text: "Student Number: ", bold: true }),
                    new TextRun("st10472501 | "),
                    new TextRun({ text: "Module: ", bold: true }),
                    new TextRun("INSY")
                ]
            }),
            
            new Paragraph({
                spacing: { after: 300 },
                children: [
                    new TextRun({ text: "Date: ", bold: true }),
                    new TextRun("September 2026")
                ]
            }),
            
            // Introduction
            new Paragraph({
                heading: HeadingLevel.HEADING_2,
                children: [new TextRun("Introduction")]
            }),
            new Paragraph({
                spacing: { after: 200 },
                children: [new TextRun("This document lists all the API endpoints that the RaceDay system will expose. The API will be built using C# with ASP.NET Core in Part 2 of this project. Each endpoint includes the HTTP method, route, description, what role can access it, what data gets sent, and what response to expect.")]
            }),
            
            // API Endpoints heading
            new Paragraph({
                heading: HeadingLevel.HEADING_2,
                children: [new TextRun("API Endpoints")]
            }),
            
            // The table
            new Table({
                width: { size: 9360, type: WidthType.DXA },
                columnWidths: colWidths,
                rows: [headerRow, ...dataRows]
            }),
            
            new Paragraph({ children: [new PageBreak()] }),
            
            // Notes section
            new Paragraph({
                heading: HeadingLevel.HEADING_2,
                children: [new TextRun("Design Notes")]
            }),
            new Paragraph({
                spacing: { after: 120 },
                children: [new TextRun("- All routes start with /api/ to keep things consistent.")]
            }),
            new Paragraph({
                spacing: { after: 120 },
                children: [new TextRun("- The system uses JWT tokens for authentication. The token is passed in the Authorization header as Bearer <token>.")]
            }),
            new Paragraph({
                spacing: { after: 120 },
                children: [new TextRun("- Role-based access is checked at the API level. If a Participant tries to access an Organiser-only endpoint, they get a 403 Forbidden response.")]
            }),
            new Paragraph({
                spacing: { after: 120 },
                children: [new TextRun("- The routes use nesting (like /api/events/{eventId}/categories) to make it clear which event a category belongs to.")]
            }),
            new Paragraph({
                spacing: { after: 120 },
                children: [new TextRun("- DELETE endpoints return 204 No Content instead of 200 because there is nothing to send back.")]
            }),
            new Paragraph({
                spacing: { after: 200 },
                children: [new TextRun("- Error responses include a message that explains what went wrong so the front-end can show it to the user.")]
            }),
            
            // AI Disclosure
            new Paragraph({
                spacing: { before: 400 },
                children: [
                    new TextRun({ text: "AI Disclosure: ", bold: true, italics: true }),
                    new TextRun({ text: "AI tools were used to assist with planning and drafting this document.", italics: true }),
                    new FootnoteReferenceRun(1)
                ]
            })
        ]
    }]
});

// Generate the file
Packer.toBuffer(doc).then(buffer => {
    fs.writeFileSync("docs/API-Endpoint-Plan.docx", buffer);
    console.log("Word document created: docs/API-Endpoint-Plan.docx");
}).catch(err => {
    console.error("Error creating document:", err);
});
