const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const WorldSchematic = require('../../src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js');
const load = (name, file) => JSON.parse(fs.readFileSync(`generated/procedural-city/final/${name}/${file}`));

for (const name of ['grid','subdivision','mixed']) {
    test(`${name}: JavaScript preserves every engine coordinate, footprint and road width`, () => {
        const world = load(name,'world-metadata.json'), city = load(name,'city.json');
        assert.equal(fs.readFileSync(`generated/procedural-city/final/${name}/schematic.js`,'utf8'),fs.readFileSync('src/WorldCreator/WorldCreator.Core/Schematic/world-schematic.js','utf8'));
        const before = JSON.stringify(world), plan = WorldSchematic.layout(world);
        assert.equal(plan.coordinateSpace,'engine-world-meters');
        assert.equal(plan.width,city.Request.WidthMeters); assert.equal(plan.height,city.Request.HeightMeters);
        assert.equal(plan.boxes.length,world.Nodes.length);
        for (const source of world.MapGeometry.Boxes) {
            const drawn = plan.boxes.find(b => b.id === source.NodeId);
            assert.deepEqual([drawn.x,drawn.y,drawn.width,drawn.height],[source.X,source.Y,source.Width,source.Height]);
        }
        for (const street of city.Streets) {
            const drawn = plan.corridors.find(r => r.owner === street.Id), [a,b] = street.Centerline.Points;
            assert.deepEqual([drawn.x1,drawn.y1,drawn.x2,drawn.y2],[a.X,a.Y,b.X,b.Y]);
            const p=street.Profile;
            assert.equal(drawn.width,p.LaneWidthMeters*p.LaneCount+p.SidewalkLeftMeters+p.SidewalkRightMeters+p.CurbLeftMeters+p.CurbRightMeters+p.MedianMeters);
        }
        assert.equal(JSON.stringify(world),before);
        world.Nodes.forEach(n => { n.Properties.schematic.columns=99; n.Properties.schematic.gap=100; });
        const again=WorldSchematic.layout(world);
        assert.deepEqual(again.boxes.map(b=>[b.x,b.y,b.width,b.height]),plan.boxes.map(b=>[b.x,b.y,b.width,b.height]));
    });
}

test('different city geometry is supplied by the C# engine, not introduced by JavaScript', () => {
    const a=load('grid','world-metadata.json'), b=load('subdivision','world-metadata.json');
    assert.notDeepEqual(a.MapGeometry.Roads,b.MapGeometry.Roads);
    assert.ok(load('mixed','city.json').Districts.some(d=>d.LayoutId==='rectilinear-grid'));
    assert.ok(load('mixed','city.json').Districts.some(d=>d.LayoutId==='recursive-neighborhoods'));
});

test('corrupt engine geometry is rejected rather than replaced by diagram packing', () => {
    for(const change of [
        w=>{ w.MapGeometry.Boxes.pop(); },
        w=>{ w.MapGeometry.Boxes[0].X=-100; },
        w=>{ w.MapGeometry.Roads[0].Width=NaN; },
        w=>{ w.Nodes[1].ParentId=w.Nodes[1].Id; }
    ]) {const world=load('grid','world-metadata.json'); change(world); assert.throws(()=>WorldSchematic.layout(world));}
});
