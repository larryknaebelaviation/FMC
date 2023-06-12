using System;
using spatial;
using System.Collections.Generic;
using flightPlanProcessing;
using System.Threading;

// Flight Management Computer
namespace flightmanagementcomputer
{
    public class FMC
    {       
        private Position lastPos;
        private VVector  lastVector; //
        private BlackBox bb;
        private FlightPlan fp;
       

        #region Properties
        public BlackBox getBlackBox() => bb;
        public void setFlightPlan(FlightPlan fp)
        {
            this.fp = fp;
        }

        public FlightPlan GetFlightPlan()
        {
            return fp;
        }

        public Position getLastPosition()
        {
            //actually need to calculate new position but not create change event
            return lastPos;
        }

        public void setAltitude(double feet)
        {
            // don't consider a change event right now TODO: perhaps later
            // Convert feet to Nautical Miles
            lastPos.setAltitude(feet * 0.00016458d);

        }
        #endregion

        #region Constructor
        public FMC(string name, Position p, VVector v)
        {
            lastPos = p;
            lastVector = v;
            bb = new BlackBox(name);
            bb.addEvent(new ChangeEvent(name,lastPos, lastVector));
        }
        #endregion

        #region Actions
        public void changeEvent(VVector v)
        {
            // process the change
            Position currentPos = getCurrentPosition();
            lastVector = v;
            lastPos = currentPos;
            if (v.getAltitude() >= 0)
            {
                setAltitude(v.getAltitude()); // converts to NM
            }           
            // record the change in the blackbox
            bb.addEvent(new ChangeEvent(bb.getName(),lastPos, lastVector));

        }

        public void activateFlightPlan()
        {
            FlightPlanSegment fps = fp.getSegment(fp.getFirstNavSegmentIndex());
            VVector v = new VVector(fps.speed, fps.heading, fps.altitudefeet, DateTime.UtcNow);
            //Console.WriteLine(fps.ToString());
            changeEvent(v);
            Thread.Sleep(Convert.ToInt32(fps.minutes * 60000));
            do
            {
                fps = fp.getNextNavSegment();
                v = new VVector(fps.speed, fps.heading, fps.altitudefeet, DateTime.UtcNow);
                changeEvent(v);
                //Console.WriteLine("fps: {0}", fps);
                Thread.Sleep(Convert.ToInt32(fps.minutes * 60000));

            } while (fps.comment != "END OF FLIGHT PLAN");
        }
        public Position getCurrentPosition()
        {
            VVector currentVector = new VVector(lastVector.getSpeed(), lastVector.getBearing(), DateTime.UtcNow);
            Position newpos = p2p(lastPos, currentVector);
            return newpos;

        }


        public void dumpHistory()
        {
            bb.dumpHistory();
        }
        #endregion

        #region Calculations
        private Position p2p(Position original, VVector vv)
        {

            /*
             *http://edwilliams.org/avform.htm#LL
             * https://stackoverflow.com/questions/378281/lat-lon-distance-heading-lat-lon
             *  lat =asin(sin(lat1)*cos(d)+cos(lat1)*sin(d)*cos(tc))
                dlon=atan2(sin(tc)*sin(d)*cos(lat1),cos(d)-sin(lat1)*sin(lat))
                lon=mod( lon1-dlon +pi,2*pi )-pi
             *
             *  angle_radians = (pi / 180) * angle_degrees
                angle_degrees = (180 / pi) * angle_radians
                distance_radians = (pi / (180 * 60)) * distance_nm
                distance_nm = ((180 * 60) / pi) * distance_radians
             */
            double lengthNM = calcLength(vv.getSpeed());
            double distance_radians = Dnm2r(lengthNM);
            double truecourseSA_radians = Ad2r(sab2d(vv.getBearing()));
            double truecourse_radians = Ad2r(vv.getBearing());
            double lat1_radians = Ad2r(original.getLatitude());
            double lon1_radians = Ad2r(original.getLongitude());
            double newlat_radians = Math.Asin(
                  Math.Sin(lat1_radians)
                * Math.Cos(distance_radians)
                + Math.Cos(lat1_radians)
                * Math.Sin(distance_radians)
                * Math.Cos(truecourse_radians)
                );
            double dlon = Math.Atan2(
                  Math.Sin(truecourse_radians) 
                * Math.Sin(distance_radians) 
                * Math.Cos(lat1_radians),
                  Math.Cos(distance_radians) 
                - Math.Sin(lat1_radians) 
                * Math.Sin(newlat_radians)
                );
            double newlon_radians = mod(lon1_radians - dlon + Math.PI, 2 * Math.PI) - Math.PI;
            Position newPos = new Position(Ar2d(newlat_radians), Ar2d(newlon_radians), original.getAltitude());
            lastPos = newPos;
            lastVector = vv;
            return newPos;
        }

        private Position p2pSimple(Position original, VVector vv)
        {
            double lengthNM = vv.getTime().Millisecond / 3600 * vv.getSpeed(); //
            double distance_radians = Dnm2r(lengthNM);
            double truecourse_radians = Ad2r(vv.getBearing());
            double lat1_radians = Ad2r(original.getLatitude());
            double lon1_radians = Ad2r(original.getLongitude());
            double newlat = Math.Asin(
                  Math.Sin(lat1_radians) 
                * Math.Cos(distance_radians)
                + Math.Cos(lat1_radians) 
                * Math.Sin(distance_radians) 
                * Math.Cos(truecourse_radians)
                );
            double newlon = 0.0d;
            if (Math.Cos(newlat) == 0)
            {
                newlon = lon1_radians;      // endpoint a pole
            }
            else
            {
                double y = (
                      lon1_radians 
                    - Math.Asin(
                        Math.Sin(truecourse_radians) 
                      * Math.Sin(distance_radians) 
                      / Math.Cos(newlat)) + Math.PI
                      );
                Console.WriteLine("Floor6/2.2:" + 6.0d / 2.2d + " " + Math.Floor(6.0d / 2.2d));
                 
                
                double x = (2 * Math.PI);
                double modVal = mod(y, x);
                newlon = modVal - Math.PI;
            }
            Position newPos = new Position(Ar2d(newlat), Ar2d(newlon), original.getAltitude());
            lastPos = newPos;
            lastVector = vv;
            return newPos;

        }

        private double calcLength(double speedK)
        {
            TimeSpan span = DateTime.UtcNow - lastVector.getTime();
            double td = span.TotalMilliseconds;
            double lengthNM = span.TotalMilliseconds / 3600000d * speedK;
            return lengthNM;
        }
        #endregion
 
        #region Conversions
        // standardAngleFromBearingToDegrees
        private double sab2d(double bearing)
        {
            double ang = bearing;
            double an1 = 0.0d;
            an1 = 90.0d - ang;
            if (an1 < 0.00d)
            {
                an1 += 360.0d;
            }

            if (an1 > 360.0d)
                an1 -= 360.0d;
            return an1;

        }


        private double Ad2r(double angle_degrees)
        {
            return angle_degrees * (Math.PI / 180d);
        }

        private double Ar2d(double angle_radians)
        {
            return (180d / Math.PI) * angle_radians;
        }

        private double Dr2nm(double distance_radians)
        {
            return ((180d * 60d) / Math.PI) * distance_radians;
        }

        private double Dnm2r(double distance_nm)
        {
            return (Math.PI / (180d * 60d)) * distance_nm;
        }

        private double mod(double y, double x)
        {
            return y - x * Math.Floor(y / x);

        }
        #endregion
    }
}

   


