using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using Oracle.ManagedDataAccess.Client;

namespace RFIDProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        // 포트설정
        private SerialPort _Port;

        private SerialPort Port
        {
            get
            {
                if (_Port == null)
                {
                    _Port = new SerialPort();
                    _Port.PortName = "COM6";
                    _Port.BaudRate = 9600;
                    _Port.DataBits = 8;
                    _Port.Parity = Parity.None;
                    _Port.Handshake = Handshake.None;
                    _Port.StopBits = StopBits.One;
                    _Port.Encoding = Encoding.UTF8;
                    _Port.DataReceived += Port_DataReceived;
                }
                return _Port;
            }
        }

        // DB연결정보
        string connect_info = "DATA SOURCE=localhost:1521/xe;PERSIST SECURITY INFO=True;USER ID=MANAGER;PASSWORD=1234";
        OracleConnection conn;
        OracleCommand cmd;

        private void Form1_Load(object sender, EventArgs e)
        {
            // 열자마자 포트연결
            if (!Port.IsOpen)
            {
                // 현재 시리얼이 연결된 상태가 아니면 연결.
                try
                {
                    // 연결
                    Port.Open();
                }
                catch (Exception ex) { label1.Text = "Error"; }
            }
            else
            {
                // 현재 시리얼이 연결 상태이면 연결 해제
                Port.Close();
                label1.Text = "Closed";
            }

            // 열자마자 오라크DB 연결
            conn = new OracleConnection(connect_info);
            cmd = new OracleCommand();
            conn.Open();
            cmd.Connection = conn;

        }


        // 테이블 보기 버튼
        private void button1_Click(object sender, EventArgs e)
        {
            // 
            string str = "select * from product_tbl";
            DataSet ds = new DataSet();
            OracleDataAdapter ad = new OracleDataAdapter();
            ad.SelectCommand = new OracleCommand(str, conn);
            ad.Fill(ds, "product_tbl");

            // 상품테이블 리스트뷰1로 보여주기
            listView1.Items.Clear();
            for (int i = 0; i < ds.Tables["product_tbl"].Rows.Count; i++)
            {
                ListViewItem item = new ListViewItem(ds.Tables["product_tbl"].Rows[i][0].ToString());
                for (int j = 1; j < ds.Tables["product_tbl"].Columns.Count; j++)
                {
                    item.SubItems.Add(ds.Tables["product_tbl"].Rows[i][j].ToString());
                }
                listView1.Items.Add(item);
            }

        }

        //RFID로 UID 읽고 리스트뷰에 추가
        string buf = null;

        private void Port_DataReceived(object sender,
            SerialDataReceivedEventArgs e)
        {
            String msg = Port.ReadExisting().Trim();

            this.Invoke(new EventHandler(delegate
            {
                buf += msg;
                if (buf.Length >= 8)		// 8 글자 모아서 표시하기
                {
                    label1.Text = buf;
                    buf = null;

                    // PRODUCT_TBL의 EPC 열에서 UID를 검색하여 나온 행을 ds에 저장
                    string str = "select epc, order_id, quant_num from product_tbl WHERE EPC ='" + 
                                    label1.Text + "'";
                    DataSet ds = new DataSet();
                    OracleDataAdapter ad = new OracleDataAdapter(str, conn);
                    ad.Fill(ds, "product_tbl");

                    // 리스트뷰2에 행추가
                    ListViewItem item = new ListViewItem(label1.Text);                    
                    for (int j = 1; j < ds.Tables["product_tbl"].Columns.Count; j++)
                    {
                        item.SubItems.Add(ds.Tables["product_tbl"].Rows[0][j].ToString());
                    }
                    item.SubItems.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                    item.SubItems.Add(DateTime.Now.ToString("HH:mm:ss"));
                    listView2.Items.Add(item);

                    str = "INSERT INTO MANAGER.POP_TBL " +
                            "VALUES('" + label1.Text + "'" +
                            ", '" + item.SubItems[1].Text + "'" +
                            ", '" + item.SubItems[2].Text + "'" +
                            ", '" + item.SubItems[3].Text + "'" +
                            ", '" + item.SubItems[4].Text + "')";
                    ds = new DataSet();
                    ad = new OracleDataAdapter(str, conn);
                    ad.Fill(ds, "pop_tbl");
             
                }

            }));
        }
    }
}
