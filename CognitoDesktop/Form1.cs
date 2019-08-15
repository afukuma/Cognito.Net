using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CognitoDesktop
{
    public partial class Form1 : Form
    {

        private string wwUserPoolID = "";
        private string wwAppClientID = "";
        private string wwIdPoolID = "";
        RegionEndpoint wwRegion;
        private string wwS3BucketName = "";


        public Form1()
        {
            InitializeComponent();
             btnSignIn.Click += BtnSignIn_ClickAsync;
       }

        private void Form1_Load(object sender, EventArgs e)
        {
            //App.Configから各種情報の取得
            wwUserPoolID = ConfigurationManager.AppSettings["UserPoolID"];
            wwAppClientID = ConfigurationManager.AppSettings["AppClientID"];
            wwIdPoolID = ConfigurationManager.AppSettings["IdPoolID"];

            wwS3BucketName = ConfigurationManager.AppSettings["S3BucketName"];

            string wwRegionString = ConfigurationManager.AppSettings["RegionString"];
            wwRegion = null;
            foreach(RegionEndpoint wR in RegionEndpoint.EnumerableAllRegions)
            {
                if (wR.SystemName == wwRegionString)
                {
                    wwRegion = wR;
                    return;
                }
            }


        }

        private async void BtnSignIn_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                AmazonCognitoIdentityProviderClient provider =
                    new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), wwRegion);
                CognitoUserPool userPool = new CognitoUserPool(wwUserPoolID, wwAppClientID, provider);
                CognitoUser user = new CognitoUser(txtID.Text, wwAppClientID, userPool, provider);
                InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
                {
                    Password = txtPass.Text
                };

                AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);
                string accessToken = authResponse.AuthenticationResult.AccessToken;
                string idToken = authResponse.AuthenticationResult.IdToken;

                // Amazon Cognito 認証情報プロバイダーを初期化します
                CognitoAWSCredentials credentials = new CognitoAWSCredentials(
                    wwIdPoolID, // ID プールの ID
                    wwRegion    // リージョン
                );

                credentials.AddLogin("cognito-idp.us-east-1.amazonaws.com/" + wwUserPoolID, idToken); // the raw token
                //↓おまじない
                string hoge = await credentials.GetIdentityIdAsync();

                using (var client = new AmazonS3Client(credentials, wwRegion))
                {
                    var listObjectRequest = new ListObjectsRequest();
                    listObjectRequest.BucketName = wwS3BucketName;
                    var response = await client.ListObjectsAsync(listObjectRequest);
                    //ここでオブジェクトがとれる
                    ;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
