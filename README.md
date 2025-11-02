# StockTrendPredictor

StockTrendPredictor är en konsolapplikation skriven i C# och .NET som använder maskininlärning för att analysera och förutsäga aktietrender baserat på historisk data. 
Projektet demonstrerar hur ML.NET och AutoML kan användas för regression och binär klassificering inom finansdata.

## Funktioner

- Hämtar historisk aktiedata från [Alpha Vantage API](https://www.alphavantage.co/).  
- Tränar två modeller:  
  - **Regressionsmodell** (AutoML) – förutsäger nästa dags stängningskurs.  
  - **Klassificeringsmodell** (FastTree) – förutsäger uppgång eller nedgång nästa dag.  
- Sparar modellernas förutsägelser i en **JSON-fil**.  
- Visar tidigare förutsägelser och en lista med populära aktier.  
- Enkel och användarvänlig konsolmeny för navigation.
