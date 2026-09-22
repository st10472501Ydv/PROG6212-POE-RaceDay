-- ============================================
-- RaceDay Database Script
-- Creates the full schema for the RaceDay system
-- Uses SQL Server Management Studio (SSMS)
-- ============================================

-- Drop tables in reverse order of dependencies
-- This lets you re-run the script without errors
IF OBJECT_ID('Results', 'U') IS NOT NULL DROP TABLE Results;
IF OBJECT_ID('Enrolments', 'U') IS NOT NULL DROP TABLE Enrolments;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Events', 'U') IS NOT NULL DROP TABLE Events;
IF OBJECT_ID('OrganiserDetails', 'U') IS NOT NULL DROP TABLE OrganiserDetails;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;

-- ============================================
-- 1. Users Table
-- Stores all user accounts (both roles)
-- ============================================
CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    DateJoined DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- 2. OrganiserDetails Table
-- Extra info for users with the Organiser role
-- ============================================
CREATE TABLE OrganiserDetails (
    OrganiserID INT PRIMARY KEY,
    ClubName NVARCHAR(100) NULL,
    PhoneNumber NVARCHAR(20) NULL,
    Bio NVARCHAR(500) NULL,
    CONSTRAINT FK_OrganiserDetails_Users FOREIGN KEY (OrganiserID)
        REFERENCES Users(UserID) ON DELETE CASCADE
);

-- ============================================
-- 3. Events Table
-- Stores all the races and events
-- ============================================
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    EventDate DATETIME NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserID)
        REFERENCES Users(UserID) ON DELETE NO ACTION
);

-- ============================================
-- 4. Categories Table
-- Each event can have multiple distance categories
-- ============================================
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    MaxParticipants INT NULL DEFAULT 0,
    EntryFee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES Events(EventID) ON DELETE CASCADE,
    CONSTRAINT CK_MaxParticipants CHECK (MaxParticipants >= 0)
);

-- ============================================
-- 5. Enrolments Table
-- When a participant signs up for a category
-- ============================================
CREATE TABLE Enrolments (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantID)
        REFERENCES Users(UserID) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID) ON DELETE CASCADE,
    CONSTRAINT UQ_ParticipantCategory UNIQUE (ParticipantID, CategoryID)
);

-- ============================================
-- 6. Results Table
-- Stores finish times and positions after events
-- ============================================
CREATE TABLE Results (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME(7) NOT NULL,
    Position INT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Finished'
        CHECK (Status IN ('Finished', 'DNF', 'Disqualified')),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID) ON DELETE CASCADE
);

-- ============================================
-- SEED DATA
-- At minimum: 2 Organisers, 2 Participants,
-- 3 Events, categories for each, sample enrolments
-- The passwords are hashed values (not real passwords)
-- ============================================

-- Insert 2 Organisers
INSERT INTO Users (FullName, Email, PasswordHash, Role, DateJoined)
VALUES
    ('Thabo Molefe', 'thabo@raceday.co.za', 'hashed_pw_123', 'Organiser', '2026-01-15'),
    ('Nadia Patel', 'nadia@raceday.co.za', 'hashed_pw_456', 'Organiser', '2026-02-10');

-- Insert their organiser details
INSERT INTO OrganiserDetails (OrganiserID, ClubName, PhoneNumber, Bio)
VALUES
    (1, 'Joburg Runners Club', '082-555-1234', 'Been organising races since 2018. Love the Comrades.'),
    (2, 'Cape Town Athletics', '083-666-5678', 'Managing Cape Town cycling and running events for 5 years.');

-- Insert 2 Participants
INSERT INTO Users (FullName, Email, PasswordHash, Role, DateJoined)
VALUES
    ('Sipho Dlamini', 'sipho@gmail.com', 'hashed_pw_789', 'Participant', '2026-03-01'),
    ('Emily van der Merwe', 'emily@gmail.com', 'hashed_pw_012', 'Participant', '2026-03-20');

-- Insert 3 Events
-- Event 1 is created by Thabo (OrganiserID = 1)
-- Event 2 is created by Nadia (OrganiserID = 2)
-- Event 3 is also by Thabo
INSERT INTO Events (OrganiserID, EventName, Description, EventDate, Location, IsActive)
VALUES
    (1, 'Soweto Marathon 2026', 'Annual marathon through the streets of Soweto. 42km, 21km and 10km options.', '2026-11-15', 'Soweto, Johannesburg', 1),
    (2, 'Cape Town Cycle Tour', 'The world最大的single-day cycling event. Routes around the Cape Peninsula.', '2026-10-08', 'Cape Town Stadium, Cape Town', 1),
    (1, 'Comrades Marathon 2027', 'The Ultimate Human Race. Pietermaritzburg to Durban.', '2027-06-13', 'Pietermaritzburg to Durban', 1);

-- Insert Categories for Event 1 (Soweto Marathon)
-- Three distance options with different entry fees and capacity limits
INSERT INTO Categories (EventID, CategoryName, MaxParticipants, EntryFee)
VALUES
    (1, '42.2km Marathon', 5000, 450.00),
    (1, '21.1km Half Marathon', 8000, 350.00),
    (1, '10km Road Run', 10000, 200.00);

-- Insert Categories for Event 2 (Cape Town Cycle Tour)
INSERT INTO Categories (EventID, CategoryName, MaxParticipants, EntryFee)
VALUES
    (2, '109km Cycle Race', 35000, 650.00),
    (2, '62km Cycle Race', 15000, 500.00);

-- Insert Categories for Event 3 (Comrades Marathon)
INSERT INTO Categories (EventID, CategoryName, MaxParticipants, EntryFee)
VALUES
    (3, '89km Up Run', 20000, 550.00),
    (3, '87km Down Run', 20000, 550.00);

-- Insert Sample Enrolments
INSERT INTO Enrolments (ParticipantID, CategoryID, EnrolmentDate)
VALUES
    (3, 1, '2026-04-01'),  -- Sipho in Soweto Marathon 42km
    (3, 5, '2026-04-05'),  -- Sipho in Cape Town Cycle 109km
    (4, 2, '2026-04-02'),  -- Emily in Soweto Marathon 21km
    (4, 3, '2026-04-10'),  -- Emily in Soweto Marathon 10km
    (4, 6, '2026-04-12'),  -- Emily in Cape Town Cycle 62km
    (3, 7, '2026-04-15');  -- Sipho in Comrades Up Run

-- Insert Sample Results (for Soweto Marathon)
INSERT INTO Results (EnrolmentID, FinishTime, Position, Status)
VALUES
    (1, '03:45:22', 156, 'Finished'),
    (3, '01:52:10', 89, 'Finished'),
    (4, '00:58:33', 23, 'Finished');

-- ============================================
-- Verify the data
-- ============================================
SELECT u.FullName, u.Role FROM Users u;
SELECT e.EventName, c.CategoryName, c.EntryFee
FROM Events e
JOIN Categories c ON e.EventID = c.EventID;
SELECT u.FullName AS Participant, e.EventName, c.CategoryName
FROM Enrolments en
JOIN Users u ON en.ParticipantID = u.UserID
JOIN Categories c ON en.CategoryID = c.CategoryID
JOIN Events e ON c.EventID = e.EventID;
