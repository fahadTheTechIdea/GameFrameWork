using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static partial class LineDefinitionContractTests
    {
        private const string MaterialCase = "Line material semantics: topology and ownership metadata cannot overwrite resource quantities";
        private static void TopologyNotMaterial(Check c)
        {
            foreach (bool independent in new[] { false, true })
            foreach (bool ownership in new[] { false, true })
            foreach (bool quantity in new[] { false, true })
            {
                LineDefinitionContractsModule proof = null;
                Exception refused = null; Beep.SimEngine.Hosting.SimGame game = null;
                Action<JsonValue> configureLine = document => {
                    if (ownership) {
                        var line = document.Get(independent ? "independent_lines" : "lines")[0];
                        line.Set("owned", JsonValue.Of(true)); line.Set("owner_layer", JsonValue.Of("segment.material-owner"));
                    }
                };
                try {
                    var factory = Create(out proof, independent, independent ? configureLine : null, infrastructureEdit: document => {
                        if (!independent) configureLine(document);
                        var layers = document.Get("spatial_layers"); JsonValue layer;
                        if (ownership) {
                            layer = Json.Parse("{\"id\":\"segment.material-owner\",\"space\":\"survey.tiles\",\"type\":\"network.ownership\",\"value_type\":\"int\",\"visibility\":\"public\",\"unit\":\"survey.count\"}");
                            layers.Add(layer);
                        }
                        else layer = layers[0];
                        layer.Set("resource", JsonValue.Of("steel"));
                        layer.Set("resource_measure", JsonValue.Of(quantity ? "quantity" : "associated"));
                    });
                    game = New(factory);
                }
                catch (Exception error) { refused = error; }
                string context = "independent=" + independent + "/ownership=" + ownership + "/quantity=" + quantity;
                if (quantity) {
                    c.That(refused is ContentException && refused.Message.Contains("quantity"), "actual material channel is rejected by the line contract: " + context + "/" + refused?.Message);
                    c.Equal(0, proof.Initialize.Initializations, "material/topology mismatch refuses before any system Initialize: " + context);
                }
                else {
                    c.That(refused == null && game != null && game.Fault == null, "associated resource metadata remains valid nonmaterial topology: " + context + "/" + refused?.Message);
                    c.Equal(1, proof.Initialize.Initializations, "valid native/direct topology initializes normally: " + context);
                }
            }
        }
    }
}
