--USE aida64;

IF exists(select * from sysobjects where id = object_id('Item') and OBJECTPROPERTY(id, 'IsTable') = 1)
DROP TABLE Item;

IF exists(select * from sysobjects where id = object_id('Report') and OBJECTPROPERTY(id, 'IsTable') = 1)
DROP TABLE Report;

IF exists(select * from sysobjects where id = object_id('NextID') and OBJECTPROPERTY(id, 'IsTable') = 1)
DROP TABLE NextID;

CREATE TABLE Report (
  ID            INT IDENTITY (1,1),
  RVersion      VARCHAR(255),
  RHost         VARCHAR(255),
  RHostComment  VARCHAR(255),
  RUser         VARCHAR(255),
  RLocation     VARCHAR(255),
  RDateTime     VARCHAR(16),
  RComplete     BIT NOT NULL,

  CONSTRAINT    cnstR1 PRIMARY KEY(ID)
);

CREATE TABLE Item (
  INum          INT,
  IPage         VARCHAR(100),
  IDevice       VARCHAR(255),
  IGroup        VARCHAR(255),
  IField        VARCHAR(255),
  IValue        VARCHAR(255),
  IIcon         INT,
  IID           INT,
  ReportID      INT NOT NULL,

  CONSTRAINT    cnstI1 FOREIGN KEY(ReportID) REFERENCES Report(ID)
);

CREATE TABLE NextID (
  TableName     VARCHAR(6) PRIMARY KEY,
  NextID        INT NOT NULL
);

INSERT INTO NextID (TableName, NextID) VALUES ('Report', 1);
