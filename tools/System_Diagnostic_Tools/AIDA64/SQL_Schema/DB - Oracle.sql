DROP TABLE Item;
DROP TABLE Report;
DROP TABLE NextID;

CREATE TABLE Report (
  ID            INTEGER PRIMARY KEY,
  RVersion      VARCHAR2(255),
  RHost         VARCHAR2(255),
  RHostComment  VARCHAR2(255),
  RUser         VARCHAR2(255),
  RLocation     VARCHAR2(255),
  RDateTime     VARCHAR2(16),
  RComplete     NUMBER(1) NOT NULL
);

CREATE TABLE Item (
  INum          INTEGER,
  IPage         VARCHAR2(100),
  IDevice       VARCHAR2(255),
  IGroup        VARCHAR2(255),
  IField        VARCHAR2(255),
  IValue        VARCHAR2(255),
  IIcon         INTEGER,
  IID           INTEGER,
  ReportID      INTEGER REFERENCES Report(ID)
);

CREATE TABLE NextID (
  TableName     VARCHAR2(6) PRIMARY KEY,
  NextID        INTEGER NOT NULL
);

INSERT INTO NextID (TableName, NextID) VALUES ('Report', 1);

COMMIT;
