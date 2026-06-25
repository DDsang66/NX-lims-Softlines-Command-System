namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    /// <summary>
    /// ISO/TR 11827:2012 Table A.1 — 纺织品纤维燃烧行为分类
    /// </summary>
    public static class BurningCategory
    {
        private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            // Paper — 纤维素纤维（烧纸味）
            ["Cotton"] = "Paper", ["Flax"] = "Paper", ["Linen"] = "Paper",
            ["Hemp"] = "Paper", ["Jute"] = "Paper", ["Ramie"] = "Paper",
            ["Viscose"] = "Paper", ["Rayon"] = "Paper", ["Modal"] = "Paper",
            ["Lyocell"] = "Paper", ["Cupro"] = "Paper", ["Paper Yarn"] = "Paper",
            ["*Regenerated cellulose fibre"] = "Paper", ["*cellulosic fibre"] = "Paper",
            ["vegetable fibres"] = "Paper",
            // Feather — 蛋白质纤维（烧羽毛/头发味）
            ["Wool"] = "Feather", ["Silk"] = "Feather", ["Mohair"] = "Feather",
            ["Cashmere"] = "Feather", ["Alpaca"] = "Feather", ["Rabbit hair"] = "Feather",
            ["Tussah"] = "Feather",
            // Black Smoke — 合成纤维（黑烟/熔融缩球）
            ["Polyester"] = "Black Smoke", ["Nylon"] = "Black Smoke",
            ["Polyamide"] = "Black Smoke", ["Acrylic"] = "Black Smoke",
            ["Modacrylic"] = "Black Smoke", ["Polypropylene"] = "Black Smoke",
            ["Polyethylene"] = "Black Smoke", ["Elastane"] = "Black Smoke",
            ["Spandex"] = "Black Smoke", ["Acetate"] = "Black Smoke",
            ["Elastodiene"] = "Black Smoke", ["Elastomultiester"] = "Black Smoke",
            ["Polyurethane"] = "Black Smoke", ["Rubber"] = "Black Smoke",
            ["Olefin"] = "Black Smoke",
        };

        public static List<string> Classify(IEnumerable<string> fiberNames)
        {
            return fiberNames
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => Map.TryGetValue(f.Trim(), out var cat) ? cat : null)
                .Where(c => c != null)
                .Distinct()
                .Cast<string>()
                .ToList();
        }
    }
}
