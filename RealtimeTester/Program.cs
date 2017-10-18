namespace RealtimeTester
{
    public class TestSSL
    {

        public static int Main(string[] args)
        {
            //string mirthhostname = "fabricrealtimerabbitmq.eastus.cloudapp.azure.com";
            string mirthhostname = "fabricrealtime.eastus.cloudapp.azure.com";
            string rabbitmqhostname = mirthhostname;

            RabbitMqTester.TestSecureConnectionToRabbitMq(rabbitmqhostname);

            MirthTester.TestSendingHL7(mirthhostname);

            return 0;
        }


    }
}