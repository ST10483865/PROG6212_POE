Create database Raceday_System;

use Raceday_System;

CREATE TABLE [User](
User_id INT IDENTITY(1,1) PRIMARY KEY,
Name Varchar(100) NOT NULL,
Contact Varchar(20) NULL,
Email	Varchar(200) NOT NULL UNIQUE,
Gender Varchar(20) NOT NULL
);

CREATE TABLE Organiser(
Organiser_ID INT IDENTITY(1,1) PRIMARY KEY,
User_ID  INT NOT NULL UNIQUE,
Organiser_name Varchar(200) NOT NULL,
Club_name Varchar(200) NOT NULL,
CONSTRAINT FK_Organiser_User FOREIGN KEY (User_ID)
        REFERENCES [User](User_ID)
        ON DELETE CASCADE
);

CREATE TABLE Participant(
Participiant_ID INT IDENTITY(1,1) PRIMARY KEY,
User_ID INT NOT NULL UNIQUE,
Medical_info Varchar(255) NULL,
CONSTRAINT FK_Participant_User FOREIGN KEY (User_ID)
        REFERENCES [User](User_ID)
        ON DELETE CASCADE
);

--Organsier 1:M Events
Create Table Event(
Event_ID INT IDENTITY(1,1) PRIMARY KEY,
Organiser_ID INT NOT NULL ,
Location Varchar(200) NOT NULL,
Date DATE NOT NULL, 
Time Time NOT NULL,
CONSTRAINT FK_Event_Organiser FOREIGN KEY (Organiser_ID)
        REFERENCES Organiser(Organiser_ID)
        ON DELETE CASCADE
);

CREATE TABLE Route (
Route_ID  INT   IDENTITY(1,1) PRIMARY KEY,
Event_ID  INT   NOT NULL,
Starting_Point VARCHAR(200)  NOT NULL,
Ending_Point   VARCHAR(200)  NOT NULL,
Distance       DECIMAL(6,2)  NOT NULL,
CONSTRAINT FK_Route_Event FOREIGN KEY (Event_ID)
        REFERENCES Event(Event_ID)
        ON DELETE CASCADE
);

CREATE TABLE Category (
Category_ID INT  IDENTITY(1,1) PRIMARY KEY,
Event_ID    INT  NOT NULL,
Category_name VARCHAR(100)  NOT NULL,   
Entry_fee   DECIMAL(8,2) NOT NULL DEFAULT 0,
Max_participants  INT   NULL,
CONSTRAINT FK_Category_Event FOREIGN KEY (Event_ID)
        REFERENCES Event(Event_ID)
        ON DELETE CASCADE
);

CREATE TABLE Registration (
Registration_ID  INT IDENTITY(1,1) PRIMARY KEY,
Category_ID      INT  NOT NULL,
Participant_ID   INT  NOT NULL,
Race_num  VARCHAR(20) NOT NULL,
Registration_date DATE NOT NULL DEFAULT GETDATE(),
 CONSTRAINT FK_Registration_Category FOREIGN KEY (Category_ID)
        REFERENCES Category(Category_ID)
        ON DELETE CASCADE,
 CONSTRAINT FK_Registration_Participant FOREIGN KEY (Participant_ID)
        REFERENCES Participant(Participant_ID)
        ON DELETE CASCADE,
 CONSTRAINT UQ_Registration_Race_num UNIQUE (Race_num)
);

CREATE TABLE Results (
Results_ID  INT  IDENTITY(1,1) PRIMARY KEY,
Registration_ID INT  NOT NULL UNIQUE,
Finish_time TIME   NULL,
Position    INT   NULL,
CONSTRAINT FK_Results_Registration FOREIGN KEY (Registration_ID)
        REFERENCES Registration(Registration_ID)
        ON DELETE CASCADE
);

-- Users 
INSERT INTO [User] (Name, Contact, Email, Gender) VALUES
('Tshepo Nkosi',    '0821234567', 'tshepo.Nkosi.@gmail.com',    'Male'),
('Lindiwe Dlamini', '0837654321', 'ntokoso.khumalo@gmail.com','Female'),
('lebo Mokoena',   '0724561234', 'lebo.mokoena@example.com',   'Male'),
('Anelisa Botha',   '0793332211', 'anelisa.botha@example.com',   'Female');
 
-- Organisers linked to next user
INSERT INTO Organiser (User_ID, Organiser_name, Club_name) VALUES
(1, 'Thabo Nkosi', 'Johannesburg Road Runners'),
(2, 'Lindiwe Dlamini', 'Cape Coastal Cycling Club');
 

-- Participants linked to next 2 Users
INSERT INTO Participant (User_ID, Medical_info) VALUES
(3, 'Healthly'),
(4, 'Asthma - carries inhaler');
 
-- Events linked to the 2 Organisers above
INSERT INTO Event (Organiser_ID, Location, Date, Time) VALUES
(1, 'Johannesburg CBD', '2026-10-04', '06:00:00'),
(1, 'Pretoria Botanical Gardens', '2026-11-15', '07:00:00'),
(2, 'Cape Town Waterfront', '2026-09-20', '06:30:00');
 

-- Routes  one per Event
INSERT INTO Route (Event_ID, Starting_Point, Ending_Point, Distance) VALUES
(1, 'Braam',       'FNB Stadium',      21.10),
(2, 'pretoria cbd',    'Loftus Versfeld',  10.00),
(3, 'V&A Waterfront',     'Camps Bay',         5.00);
 

-- Categories - at least one per Event (each Event gets 2 here)
INSERT INTO Category (Event_ID, Category_name) VALUES
(1, '21km Half Marathon'),
(1, '10km Fun Run'),
(2, '10km Road Race'),
(2, '5km Walk'),
(3, '5km Fun Ride'),
(3, '15km Road Ride');
 

-- Registrations (sample enrolments) - Participants entering Categories
INSERT INTO Registration (Category_ID, Participant_ID, Race_num, Registration_date) VALUES
(1, 1, 'JHB-0001', '2026-09-01'),
(3, 1, 'PTA-0001', '2026-09-10'),
(5, 2, 'CPT-0001', '2026-08-25'),
(2, 2, 'JHB-0002', '2026-09-05');
 

-- Results - sample results for completed registrations
INSERT INTO Results (Registration_ID, Finish_time, Position) VALUES
(301, '00:24:15', 1),
(499, '01:02:40', 45);