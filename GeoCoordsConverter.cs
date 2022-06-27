using System.Numerics;
using DotSpatial.Projections;

namespace s1
{
    public class GeoCoordsConverter
    {
        private readonly ProjectionInfo _mapProjectionInfo;
        private readonly ProjectionInfo _sceneProjectionInfo;
        public string SceneProj4 =
                "+proj=sterea +lat_0=37.420806 +lon_0=-122.084197 +k=1 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs";

            public string MapProj4 = "+proj=longlat +datum=WGS84 +no_defs";

            public GeoCoordsConverter(double lat, double lon)
        {
            _mapProjectionInfo = ProjectionInfo.FromProj4String(MapProj4);
            _sceneProjectionInfo = ProjectionInfo.FromProj4String("+proj=sterea +lat_0=" + lat +
                                                                  "+lon_0=" + lon + 
                                                                  "+k=1 +x_0=0 +y_0=0 +datum=WGS84 +units=m +no_defs" );
        }

        private readonly double[] Xy = new double[2];
        private readonly double[] Z = new double[1];


        public Vector3 MapToScene(Data.GPSCoords geoCoords)
        {
            Xy[0] = geoCoords.Longitude;
            Xy[1] = geoCoords.Latitude;
            Z[0] = geoCoords.Height;

            Reproject.ReprojectPoints(Xy, Z, _mapProjectionInfo, _sceneProjectionInfo, 0, Z.Length);
            return new Vector3((float) Xy[0], (float) Xy[1], (float) Z[0]);
        }
    }
}