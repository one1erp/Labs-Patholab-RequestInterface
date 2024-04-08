using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RequestInterface
{
    public class Patient
    {
        private string idType;
        private string _tz;
        private string _birthDate;
        public string FirstName { get; private set; }

        public string LastName { get; private set; }

        string _gender;

        public string Gender
        {

            get
            {
                if (_gender == "M")
                {
                    return "F";
                }
                else if (_gender == "0")
                { return "F"; }
                else
                {
                    return "U";
                }
            }
        }


        public PatientIDcode IdType
        {
            get
            {
                return (PatientIDcode)Enum.Parse(typeof(PatientIDcode), idType);

            }
        }

        public string isPassportStr
        {
            get
            {
                bool ispassport = (IdType == PatientIDcode.passport);
                var pyn = ispassport ? "T" : "F";


                return pyn;
            }
        }

        public string TZ
        {
            get
            {
                return OrganizeIdentity(_tz);
            }

        }
        public Patient(MainHeader mh)
        {
            FirstName = mh.PatFirstName;
            LastName = mh.PatLastName;
            idType = mh.IDPatCode;
            _tz = mh.PatID;
            _gender = mh.Sex;
            _birthDate = mh.BD;
        }


        private string OrganizeIdentity(string cn)
        {
            cn = cn.Trim();


            if (IdType == PatientIDcode.passport)
            {
                return cn;
            }
            else // (IdType == PatientIDcode.tz)
            {
                var cn0 = cn;
                while (cn0.Length < 9)
                {
                    cn0 = "0" + cn0;
                }
                return cn0;
            }


        }
        public DateTime? GetDate
        {

            get
            {


                try
                {
                    var dttimefull = DateTime.ParseExact(_birthDate, "ddMMyyyy", null);
                    Console.WriteLine(dttimefull.ToString());
                    return dttimefull;
                }
                catch
                {
                    return null;
                }


            }
        }
    }

    public enum PatientIDcode
    {
        tz = 1,//	ת.ז
        Anonymous = 3,//	אלמוני
        Other = 8,//	אחר
        passport = 9,
        newborn = 10//	יילוד

    }
}
