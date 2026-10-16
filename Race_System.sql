CREATE DATABASE Raceday_System;

USE Raceday_System;

CREATE TABLE [User] (
    User_ID       INT IDENTITY(1,1) PRIMARY KEY,
    Name          VARCHAR(100) NOT NULL,
    Contact       VARCHAR(20)  NULL,
    Email         VARCHAR(200) NOT NULL UNIQUE,
    Gender        VARCHAR(20)  NOT NULL,
    Password_Hash VARCHAR(255) NOT NULL
);
 
CREATE TABLE Organiser (
    Organiser_ID   INT IDENTITY(1,1) PRIMARY KEY,
    User_ID        INT NOT NULL UNIQUE,
    Organiser_name VARCHAR(200) NOT NULL,
    Club_name      VARCHAR(200) NOT NULL,
    CONSTRAINT FK_Organiser_User FOREIGN KEY (User_ID)
        REFERENCES [User](User_ID) ON DELETE CASCADE
);
 
CREATE TABLE Participant (
    Participant_ID INT IDENTITY(1,1) PRIMARY KEY,
    User_ID        INT NOT NULL UNIQUE,
    Medical_info   VARCHAR(255) NULL,
    CONSTRAINT FK_Participant_User FOREIGN KEY (User_ID)
        REFERENCES [User](User_ID) ON DELETE CASCADE
);
 
-- Organiser 1:M Event
CREATE TABLE Event (
    Event_ID     INT IDENTITY(1,1) PRIMARY KEY,
    Organiser_ID INT NOT NULL,
    Name         VARCHAR(200)  NOT NULL,
    Description  VARCHAR(1000) NULL,
    Event_type   VARCHAR(10)   NOT NULL CHECK (Event_type IN ('Run', 'Walk', 'Cycle')),
    Location     VARCHAR(200)  NOT NULL,
    Date         DATE NOT NULL,
    Time         TIME NOT NULL,
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (Organiser_ID)
        REFERENCES Organiser(Organiser_ID) ON DELETE CASCADE
);
 
-- Event 1:M Route
CREATE TABLE Route (
    Route_ID       INT IDENTITY(1,1) PRIMARY KEY,
    Event_ID       INT NOT NULL,
    Starting_Point VARCHAR(200) NOT NULL,
    Ending_Point   VARCHAR(200) NOT NULL,
    Distance       DECIMAL(6,2) NOT NULL,
    CONSTRAINT FK_Route_Event FOREIGN KEY (Event_ID)
        REFERENCES Event(Event_ID) ON DELETE CASCADE
);
 
-- Event 1:M Category
CREATE TABLE Category (
    Category_ID      INT IDENTITY(1,1) PRIMARY KEY,
    Event_ID         INT NOT NULL,
    Category_name    VARCHAR(100) NOT NULL,
    Entry_fee        DECIMAL(8,2) NOT NULL DEFAULT 0,
    Max_participants INT NULL,
    CONSTRAINT FK_Category_Event FOREIGN KEY (Event_ID)
        REFERENCES Event(Event_ID) ON DELETE CASCADE
);
 
-- Category 1:M Registration, Participant 1:M Registration
CREATE TABLE Registration (
    Registration_ID   INT IDENTITY(1,1) PRIMARY KEY,
    Category_ID       INT NOT NULL,
    Participant_ID    INT NOT NULL,
    Race_num          VARCHAR(20) NOT NULL,
    Registration_date DATE NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Registration_Category FOREIGN KEY (Category_ID)
        REFERENCES Category(Category_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Registration_Participant FOREIGN KEY (Participant_ID)
        REFERENCES Participant(Participant_ID) ON DELETE NO ACTION,
    CONSTRAINT UQ_Registration_Race_num UNIQUE (Race_num)
);
 
-- Registration 1:1 Results
CREATE TABLE Results (
    Results_ID      INT IDENTITY(1,1) PRIMARY KEY,
    Registration_ID INT NOT NULL UNIQUE,
    Finish_time     TIME NULL,
    Position        INT  NULL,
    CONSTRAINT FK_Results_Registration FOREIGN KEY (Registration_ID)
        REFERENCES Registration(Registration_ID) ON DELETE CASCADE
);
 
-- Accounts that can actually log in are created through POST /api/auth/register,

INSERT INTO [User] (Name, Contact, Email, Gender, Password_Hash) VALUES
('Tshepo Nkosi',    '0821234567', 'tshepo.nkosi@gmail.com',    'Male',   'SEED_PLACEHOLDER_NOT_A_LOGIN_HASH'),
('Lindiwe Dlamini', '0837654321', 'lindiwe.dlamini@gmail.com', 'Female', 'SEED_PLACEHOLDER_NOT_A_LOGIN_HASH'),
('Lebo Mokoena',    '0724561234', 'lebo.mokoena@example.com',  'Male',   'SEED_PLACEHOLDER_NOT_A_LOGIN_HASH'),
('Anelisa Botha',   '0793332211', 'anelisa.botha@example.com', 'Female', 'SEED_PLACEHOLDER_NOT_A_LOGIN_HASH');
 
INSERT INTO Organiser (User_ID, Organiser_name, Club_name) VALUES
(1, 'Tshepo Nkosi',    'Johannesburg Road Runners'),
(2, 'Lindiwe Dlamini', 'Cape Coastal Cycling Club');
 
INSERT INTO Participant (User_ID, Medical_info) VALUES
(3, 'Healthy'),
(4, 'Asthma - carries inhaler');
 
INSERT INTO Event (Organiser_ID, Name, Description, Event_type, Location, Date, Time) VALUES
(1, 'Joburg City Half Marathon', 'Half marathon and fun run through the Johannesburg CBD.', 'Run',   'Johannesburg CBD',          '2026-10-04', '06:00:00'),
(1, 'Pretoria Garden Run',       '10km road race and 5km family walk.',                   'Run',   'Pretoria Botanical Gardens', '2026-11-15', '07:00:00'),
(2, 'Waterfront Ride',           'Scenic Atlantic seaboard fun ride and road ride.',      'Cycle', 'Cape Town Waterfront',       '2026-09-20', '06:30:00');
 
INSERT INTO Route (Event_ID, Starting_Point, Ending_Point, Distance) VALUES
(1, 'Braamfontein',    'FNB Stadium',     21.10),
(2, 'Pretoria CBD',    'Loftus Versfeld', 10.00),
(3, 'V&A Waterfront',  'Camps Bay',       15.00);
 
INSERT INTO Category (Event_ID, Category_name, Entry_fee, Max_participants) VALUES
(1, '21km Half Marathon', 250.00, 2000),
(1, '10km Fun Run',       150.00, 3000),
(2, '10km Road Race',     150.00, 1500),
(2, '5km Walk',            80.00, 1000),
(3, '5km Fun Ride',       100.00,  800),
(3, '15km Road Ride',     180.00,  600);
 
INSERT INTO Registration (Category_ID, Participant_ID, Race_num, Registration_date) VALUES
(1, 1, 'RD001-0001', '2026-09-01'),
(3, 1, 'RD002-0001', '2026-09-10'),
(5, 2, 'RD003-0001', '2026-08-25'),
(2, 2, 'RD001-0002', '2026-09-05');
 
-- Results only for events that have already taken place (registrations 3 and 4).
INSERT INTO Results (Registration_ID, Finish_time, Position) VALUES
(3, '00:24:15', 1),
(4, '01:02:40', 45);
GO
 
SELECT * FROM Event;
SELECT * FROM Route;
SELECT * FROM Category;

SELECT e.Event_ID, e.Name, e.Event_type, r.Distance, c.Category_name
FROM Event e
JOIN Route r ON r.Event_ID = e.Event_ID
JOIN Category c ON c.Event_ID = e.Event_ID;
