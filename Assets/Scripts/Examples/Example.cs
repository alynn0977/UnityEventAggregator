// The following are examples of how to use this with another MB script

// Legacy System A - loads molecule data
_aggregator.UpdateState("LegacySystemA", LoadingPhases.Started, 
    LoadingCategory.Data | LoadingCategory.Styling, 0f, "Loading molecules...");

// Analytics system - processes molecules  
_aggregator.UpdateState("AnalyticsEngine", LoadingPhases.Started,
    LoadingCategory.Analytics | LoadingCategory.Molecules, 0f, "Analyzing...");

// Styling system - changes molecule appearance
_aggregator.UpdateState("StylingSystem", LoadingPhases.Started,
    LoadingCategory.Styling | LoadingCategory.GIS | LoadingCategory.Visuals, 
    0f, "Applying styles...");