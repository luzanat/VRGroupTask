CREATE DATABASE VRGroupChallenge;
GO

USE VRGroupChallenge;
GO

CREATE TABLE Boxes (
	Identifier VARCHAR(20) NOT NULL,
	SupplierIdentifier VARCHAR(10) NOT NULL,

	CONSTRAINT PK_Box PRIMARY KEY (Identifier)
)

CREATE TABLE BoxContent (
	BoxIdentifier VARCHAR(20) NOT NULL,
	Isbn VARCHAR(13) NOT NULL,
	PoNumber VARCHAR(10) NOT NULL,
	Quantity INT NOT NULL

	CONSTRAINT PK_BoxContent PRIMARY KEY (BoxIdentifier, Isbn)
	CONSTRAINT FK_BoxContent_Boxes FOREIGN KEY (BoxIdentifier) REFERENCES Boxes(Identifier)
)