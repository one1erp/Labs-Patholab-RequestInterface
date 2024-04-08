using Microsoft.Win32;
using Oracle.ManagedDataAccess.Client;
//using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RequestInterface;
using RequestInterfaceHelpers;
using Patholab_DAL_V1;

namespace RequestInterface
{
    public class Program
    {

        private static DataLayer _dal;
        static string NautConStr;
        private static string InputPath;
        private static string OuputPath;

        static void Main(string[] args)
        {

            SetAppSettings();


            _dal = new DataLayer();
            _dal.MockConnect(NautConStr);




            //     _dal = new DataLayer();
            //   _dal.MockConnect(NautConStr);

            READ_XML();

            //    _dal.Close();
            _dal = null;
            return;
        }

        private static void SetAppSettings()
        {
            NautConStr = ConfigurationManager.ConnectionStrings["NautConnectionString"].ConnectionString;
            InputPath = ConfigurationManager.AppSettings["InputPath"];
            OuputPath = ConfigurationManager.AppSettings["OuputPath"];


        }



        private static void Exit(string p)
        {
            Console.WriteLine("Exit Program");
            Console.WriteLine(p);
            Console.WriteLine("Press any key to continue");
            Console.Read();
            //CloseDBConnection();
        }

        private static void READ_XML()
        {
            string xmlDir = InputPath;
            var files = Directory.GetFiles(xmlDir, "*.xml");
            foreach (var item in files)
            {
                try
                {
                    string xml = File.ReadAllText(item);
                    var xmlObj = xml.ParseXML<Main>();
                    //Msg msg = ConvertXmlToNautObj(xmlObj);
                    var NewIdNbr = InsertContainerMSGFF(xmlObj);
                    //var NewIdNbr = InsertContainerMSGFF(xmlObj);
                    //var newDest = MoveFile(OuputPath, item);
                    //UpdateAsSuccess(NewIdNbr, newDest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error on Parse XML path " + item);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Press any key to continue");
                    Console.Read();
                }

            }



        }
        static long InsertContainerMSGFF(Main msg)
        {
            var id = _dal.GetNewId("SQ_U_SAMPLE_MSG");
            long NewId = Convert.ToInt64(id);
            U_SAMPLE_MSG req = new U_SAMPLE_MSG()
            {
                NAME = NewId.ToString(),
                U_SAMPLE_MSG_ID = NewId,
                VERSION = "1",
                VERSION_STATUS = "A"
            };
            decimal temp;
            // typecast either 'temp' or 'null'
            //decimal? numericValue =
            //  decimal.TryParse(msg.TRCONTNUM, out temp) ? temp : (decimal?)null;
            //string status = msg.TRCONTSTS.ToString();
            //DateTime? date = GetDate(msg.XMLDATE, msg.XMLHR);
            //DateTime? pckdate = GetDate(msg.TRDATEPICK, msg.TRTIMEPICK, "ddMMyyyyHmm");

            //long? driver = GetDriver(msg.TRDRIVER);

            var header = msg.Header;
            string CaseFile = header.CaseFile.ToString();
            string ReqNumLab = header.ReqNumLab.ToString();

            long? sender = GetSenderClinic(header.HosCode, header.UnitCode);

            Patient ptnt = new Patient(header);
            var clientid = CheckClient(ptnt);

            var refDocid = GetSupplier(header.RelatedDocID);
            var execDocid = GetSupplier(header.ExecDocID);

            string errors = GetErrors(sender, header.SUPCode, refDocid, execDocid, clientid);


            //GetClient
            //GetClinic
            //GetRefDoc
            //GetImpDoc


            U_SAMPLE_MSG_USER reqUser = new U_SAMPLE_MSG_USER()
            {
                U_SAMPLE_MSG_ID = NewId,
                U_CASE_FILE = CaseFile,
                U_REQUEST_NUM = ReqNumLab,
                U_CLIENT_ID = clientid,
                U_CLINIC_ID = sender,
                U_IMPLEMENTING_PHYSICIAN = execDocid,
                U_REFERRING_PHYSICIAN = refDocid,
                U_ERRORS = errors

            };


            _dal.Add(req);
            _dal.Add(reqUser);
            //foreach (var item in msg.TRREQNUM)
            //{
            //    InsertContainerMSG_EntryFF(item, NewId);
            //}

            _dal.SaveChanges();

            return NewId;
        }


        static long? CheckClient(Patient ptnt)
        {


            var client = _dal.GetAll<CLIENT>().FirstOrDefault(a => a.NAME == ptnt.TZ
                && a.CLIENT_USER.U_PASSPORT == ptnt.isPassportStr);
            if (client == null)
            {
                long? c = AddClient(ptnt);
                return c;
            }
            else
            {
                return client.CLIENT_ID;
            }

            //בדיקות קיום נבדק
            //   אם קיימת רשומת נבדק		Client User	
            //   עם פרמטרים זהים:
            //       1 – Client Type  =  IDPatCode
            //   וגם	2 – u_id_code	  =  PatID
            //   וגם	3 – u_gender    =  Sex 
            //   וגם	4 - U_FIRST_NAME = PatFirstName
            //   וגם	5 - U_LAST_NAME = PatLastName  
            //   ---- >	הנבדק  קיים בבסיס הנתונים
            //   אם לא מתקיים עבור כל 5 הבדיקות
            //       אבל כן מתקיים פרמטרים 	1 + 2 
            //       ---- >	מחייב אישור ידני של קיום הנבדק
            //   אם לא מתקיים 1 + 2
            //       ---- >  נבדק חדש
            //   הקם נבדק חדש בבסיס הנתונים, עם הפרמטרים  1 – 5          
        }

        private static long? AddClient(Patient ptnt)
        {
            CLIENT _currentClient;
            if (ptnt.IdType == PatientIDcode.tz)
            {
                bool validId = _dal.Run_CheckId(ptnt.TZ);
                if (!validId)
                {
                    return null;
                }
            }


            _currentClient = new CLIENT();
            _currentClient.CLIENT_USER = new CLIENT_USER();
            _currentClient.NAME = ptnt.TZ;


            var clientId = _dal.GetNewId("SQ_CLIENT");
            _currentClient.CLIENT_ID = (long)clientId;
            _currentClient.CLIENT_USER.CLIENT_ID = (long)clientId;
            _currentClient.VERSION = "1";
            _currentClient.VERSION_STATUS = "A";




            _currentClient.CLIENT_USER.U_FIRST_NAME = ptnt.FirstName;
            _currentClient.CLIENT_USER.U_LAST_NAME = ptnt.LastName;


            _currentClient.CLIENT_USER.U_DATE_OF_BIRTH = ptnt.GetDate;


            _currentClient.CLIENT_USER.U_PASSPORT = ptnt.isPassportStr;

            _currentClient.CLIENT_USER.U_GENDER = ptnt.Gender;

            _dal.Add(_currentClient);


            _dal.SaveChanges();
            return _currentClient.CLIENT_ID;





        }




        static void UpdateAsSuccess(long NewIdNbr, string newDest)
        {

            U_CONTAINER_MSG_USER msg = _dal.FindBy<U_CONTAINER_MSG_USER>(x => x.U_CONTAINER_MSG_ID == NewIdNbr).FirstOrDefault();
            if (msg != null)
            {
                msg.U_PATH = newDest;
            }
            _dal.SaveChanges();
        }

        private static string MoveFile(string xmlDir, string file)
        {

            string NameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            FileInfo f = new FileInfo(file);
            var bb = GetCreateMyFolder(xmlDir);
            var newDest = Path.Combine(bb.FullName, NameWithoutExtension + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
            File.Move(file, newDest);
            return newDest;
        }

        private static string GetErrors(long? sender, string supCode, long? refDocid, long? execDocid, long? clientid)
        {
            string errors = "";
            if (!sender.HasValue)
            {
                errors += "No Clinic;";
            }
            if (supCode != "AS")
            {
                errors += "Isn't Assuta;";
            }
            if (!refDocid.HasValue)
            {
                errors += "No Reffering phisician;";
            }
            if (!execDocid.HasValue)
            {
                errors += "No Executing phisician;";

            }
            if (!clientid.HasValue)
            {
                errors += "No Patient";

            }

            return errors;
        }



        static long? GetDriver(string driverCode)
        {


            var DRIVER = _dal.GetAll<U_DRIVER_USER>().FirstOrDefault(x => x.U_CODE == driverCode);
            if (DRIVER != null)
            {
                return DRIVER.U_DRIVER_ID;

            }
            return null;
        }

        private static long? GetSenderClinic(string site, string unit)
        {

            var clinic = _dal.GetAll<U_CLINIC_USER>()
                .FirstOrDefault(x => x.U_ASSUTA_CLINIC_CODE == site && x.U_ASSUTA_DIVISION_CODE == unit);
            if (clinic != null)
            {
                return clinic.U_CLINIC_ID;

            }
            return null;
        }
        private static long? GetSupplier(string idNbr)
        {
            var supp = _dal.GetAll<SUPPLIER_USER>().FirstOrDefault(x => x.U_LICENSE_NBR == idNbr);
            if (supp != null)
            {
                return supp.SUPPLIER_ID;

            }
            return null;

        }

        public static DirectoryInfo GetCreateMyFolder(string baseFolder)
        {
            var now = DateTime.Now;
            var yearName = now.ToString("yyyy");
            var monthName = now.ToString("MM");
            var dayName = now.ToString("dd-MM-yyyy");

            var folder = Path.Combine(baseFolder, Path.Combine(yearName, monthName));

            return Directory.CreateDirectory(folder);
        }





    }

}
