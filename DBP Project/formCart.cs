using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace DBP_Project
{
    public partial class formCart : Form
    {
        private string strConnectionString;

        private List<DataRow> cart;
        private List<DataRow> selectedItems = new List<DataRow>(); // รายการสินค้าที่ถูกเลือก
        private Dictionary<int, Tuple<DataRow, int>> uniqueCartItems = new Dictionary<int, Tuple<DataRow, int>>(); // เพิ่ม Dictionary นี้

        public formCart(List<DataRow> cartItems)
        {
            InitializeComponent();
            // โหลดค่า Connection String
            if (System.IO.File.Exists("ConnectionString.ini"))
            {
                strConnectionString = System.IO.File.ReadAllText("ConnectionString.ini", Encoding.GetEncoding("Windows-874"));
            }
            else
            {
                MessageBox.Show("Connection string file is missing or invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            cart = cartItems;
            DisplayCartItems();


        }

        private void DisplayCartItems()
        {
            flowLayoutPanelCart.Controls.Clear(); // ลบสินค้าก่อนหน้า

            // สร้าง Dictionary เพื่อเก็บสินค้าและจำนวน
            uniqueCartItems.Clear(); // เคลียร์ Dictionary ก่อนใช้งานใหม่
            foreach (DataRow row in cart)
            {
                int productId = Convert.ToInt32(row["Product_ID"]);
                if (uniqueCartItems.ContainsKey(productId))
                {
                    // หากสินค้านี้มีอยู่แล้วในตะกร้าให้เพิ่มจำนวน
                    uniqueCartItems[productId] = new Tuple<DataRow, int>(row, uniqueCartItems[productId].Item2 + 1);
                }
                else
                {
                    // ถ้าไม่มีสินค้านี้ในตะกร้าให้เพิ่มสินค้าใหม่
                    uniqueCartItems.Add(productId, new Tuple<DataRow, int>(row, 1));
                }
            }

            // แสดงสินค้าทุกชิ้นในตะกร้า
            foreach (var item in uniqueCartItems)
            {
                DataRow row = item.Value.Item1;
                int quantity = item.Value.Item2;  // ใช้ข้อมูล quantity ที่คำนวณได้จาก dictionary
                decimal price = Convert.ToDecimal(row["Product_Price"]);

                // สร้าง Panel สำหรับสินค้าในตะกร้า
                Panel productPanel = new Panel();
                productPanel.Size = new Size(450, 170); // ขนาดของ Panel
                productPanel.BorderStyle = BorderStyle.FixedSingle; // เพิ่มกรอบให้กับ Panel

                // สร้าง PictureBox สำหรับแสดงรูปภาพ
                PictureBox productImage = new PictureBox();
                productImage.Size = new Size(150, 150); // ขนาดของรูปภาพ
                productImage.Location = new Point(10, 20); // ตำแหน่งของรูปภาพใน Panel
                productImage.SizeMode = PictureBoxSizeMode.StretchImage;

                // แสดงรูปภาพจากฐานข้อมูล
                if (row["Product_Image"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])row["Product_Image"];
                    productImage.Image = ByteArrayToImage(imageData);
                }
                else
                {
                    productImage.Image = null;
                }

                // สร้าง Label สำหรับชื่อสินค้า (ภาษาอังกฤษ)
                Label productNameLabel = new Label();
                productNameLabel.Text = "Product: " + row["Product_Name"].ToString();
                productNameLabel.TextAlign = ContentAlignment.MiddleLeft;
                productNameLabel.Location = new Point(170, 20); // ตำแหน่ง Label ใต้รูปภาพ
                productNameLabel.Size = new Size(270, 30);

                // สร้าง Label สำหรับราคา (ภาษาอังกฤษ)
                Label productPriceLabel = new Label();
                productPriceLabel.Text = "Price: $" + price.ToString("F2");
                productPriceLabel.TextAlign = ContentAlignment.MiddleLeft;
                productPriceLabel.Location = new Point(170, 60); // ตำแหน่ง Label ใต้ชื่อสินค้า
                productPriceLabel.Size = new Size(180, 30);

                // สร้าง Label สำหรับจำนวนที่ซื้อ (ภาษาอังกฤษ)
                Label productQuantityLabel = new Label();
                productQuantityLabel.Text = "Quantity: " + quantity.ToString();
                productQuantityLabel.TextAlign = ContentAlignment.MiddleLeft;
                productQuantityLabel.Size = new Size(230, 30);
                productQuantityLabel.Location = new Point(170, 100);

                // ปุ่ม "เลือกสินค้า"
                CheckBox selectItemCheckBox = new CheckBox();
                selectItemCheckBox.Text = "Select";
                selectItemCheckBox.Location = new Point(170, 130);
                selectItemCheckBox.Size = new Size(180, 30);
                selectItemCheckBox.CheckedChanged += (sender, e) =>
                {
                    if (selectItemCheckBox.Checked)
                    {
                        // เมื่อเลือกสินค้าให้เพิ่มสินค้าลงใน selectedItems
                        for (int i = 0; i < quantity; i++)
                        {
                            selectedItems.Add(row);
                        }
                    }
                    else
                    {
                        // เมื่อยกเลิกการเลือกสินค้าให้ลบสินค้าจาก selectedItems
                        for (int i = 0; i < quantity; i++)
                        {
                            selectedItems.Remove(row);
                            checkBoxAll.Checked = false;
                        }
                    }

                    // คำนวณราคารวมของสินค้าที่เลือก
                    CalculateTotalPrice();

                    // ตรวจสอบว่า Select All ควรจะถูกติ๊กหรือไม่

                };

                // ปุ่ม "ลบสินค้า"
                Button deleteItemButton = new Button();
                deleteItemButton.Text = "Delete";
                deleteItemButton.Location = new Point(350, 130); // ตำแหน่งปุ่ม
                deleteItemButton.Size = new Size(75, 30);
                deleteItemButton.Click += (sender, e) =>
                {
                    // ลบสินค้าทั้งหมดที่ตรงกันจากตะกร้า
                    cart.RemoveAll(c => c["Product_ID"].ToString() == item.Value.Item1["Product_ID"].ToString());

                    // ลบสินค้าจากรายการที่เลือก
                    selectedItems.RemoveAll(s => s["Product_ID"].ToString() == item.Value.Item1["Product_ID"].ToString());

                    // คำนวณราคารวมหลังจากลบสินค้า
                    CalculateTotalPrice();

                    // ลบสินค้าเก่าใน FlowLayoutPanel
                    flowLayoutPanelCart.Controls.Clear();

                    // แสดงสินค้าหลังจากการลบ
                    DisplayCartItems();
                };

                // เพิ่ม PictureBox, Labels, CheckBox, และปุ่มลบลงใน Panel
                productPanel.Controls.Add(productImage);
                productPanel.Controls.Add(productNameLabel);
                productPanel.Controls.Add(productPriceLabel);
                productPanel.Controls.Add(productQuantityLabel);
                productPanel.Controls.Add(selectItemCheckBox);
                productPanel.Controls.Add(deleteItemButton);

                // เพิ่ม Panel ลงใน FlowLayoutPanel (เพื่อแสดงสินค้าในแนวนอน)
                flowLayoutPanelCart.Controls.Add(productPanel);
            }
        }

        private Image ByteArrayToImage(byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
                return null;

            using (MemoryStream ms = new MemoryStream(byteArray))
            {
                return Image.FromStream(ms);
            }
        }

        // ฟังก์ชันคำนวณราคารวมของสินค้าที่เลือก
        private void CalculateTotalPrice()
        {
            decimal totalPrice = 0;

            // ตรวจสอบสินค้าที่ถูกเลือก
            foreach (var selectedItem in selectedItems.Distinct()) // ใช้ Distinct() เพื่อไม่ให้คำนวณสินค้าซ้ำ
            {
                if (selectedItem["Product_Price"] != DBNull.Value)
                {
                    decimal price = Convert.ToDecimal(selectedItem["Product_Price"]);
                    int productId = Convert.ToInt32(selectedItem["Product_ID"]);
                    int quantity = uniqueCartItems[productId].Item2; // ดึงจำนวนสินค้าจาก uniqueCartItems

                    totalPrice += price * quantity; // คำนวณราคาโดยรวมจำนวนสินค้า
                }
            }

            // แสดงราคารวม
            labelTotalPrice.Text = "Total: $" + totalPrice.ToString("F2");
        }

        private void checkBoxAll_CheckedChanged(object sender, EventArgs e)
        {
            selectedItems.Clear(); // ล้างรายการสินค้าที่เลือกทั้งหมดก่อน

            if (checkBoxAll.Checked)
            {
                // ทำการติ๊กเช็คบ็อกซ์ทุกตัวที่เกี่ยวข้องในตะกร้า
                foreach (Control control in flowLayoutPanelCart.Controls)
                {
                    if (control is Panel panel)
                    {
                        CheckBox itemCheckBox = panel.Controls.OfType<CheckBox>().FirstOrDefault();
                        if (itemCheckBox != null)
                        {
                            itemCheckBox.Checked = true; // ทำการติ๊กทุกตัว
                        }
                    }
                }
            }
            else
            {
                // ยกเลิกการติ๊กเช็คบ็อกซ์ทุกตัวที่เกี่ยวข้อง
                foreach (Control control in flowLayoutPanelCart.Controls)
                {
                    if (control is Panel panel)
                    {
                        CheckBox itemCheckBox = panel.Controls.OfType<CheckBox>().FirstOrDefault();
                        if (itemCheckBox != null)
                        {
                            itemCheckBox.Checked = false; // ยกเลิกการติ๊กทุกตัว
                        }
                    }
                }

                // เมื่อยกเลิก Select All ให้ลบสินค้าทุกตัวออกจาก selectedItems
                selectedItems.Clear();
            }

            // คำนวณราคารวมหลังจากเลือกทั้งหมดหรือยกเลิก
            CalculateTotalPrice();
        }

        private void buttonCheckout_Click(object sender, EventArgs e)
        {
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("กรุณาเลือกสินค้าก่อนที่จะไปยังหน้าต่อไป", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // หยุดการทำงานเมื่อไม่มีสินค้าที่เลือก
            }
            int memberPhone;
            while (true)
            {
                // แสดง InputBox ให้ผู้ใช้กรอก Member ID
                string input = Interaction.InputBox("กรุณากรอกเบอร์โทรศัพท์สมาชิก:", "กรอกข้อมูลสมาชิก", "");

                // ตรวจสอบว่าผู้ใช้กดปิดหรือไม่ได้กรอกอะไรเลย
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("กรุณากรอกเบอร์โทรศัพท์สมาชิก", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ตรวจสอบว่ากรอกเป็นตัวเลขหรือไม่
                if (int.TryParse(input, out memberPhone))
                {
                    // เชื่อมต่อกับฐานข้อมูลเพื่อดึงสถานะของสมาชิก
                    using (SqlConnection conn = new SqlConnection(strConnectionString))
                    {
                        conn.Open();
                        string query = "SELECT Status FROM Member WHERE Member_Phone = @MemberPhone";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@MemberPhone", memberPhone);

                            var result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                string status = result.ToString();
                                if (status == "Lock")
                                {
                                    MessageBox.Show("บัญชีของลูกค้าถูกล็อก กรุณาติดต่อผู้ดูแลระบบ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return; // หยุดการทำงานถ้าบัญชีถูกล็อก
                                }
                            }
                            else
                            {
                                MessageBox.Show("ไม่พบข้อมูลลูกค้าในระบบ", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }
                    }

                    CurrentUser.MemberID = memberPhone;
                    break; // ถ้ากรอกถูกต้อง ให้ออกจากลูป
                }
                else
                {
                    MessageBox.Show("กรุณากรอกเฉพาะตัวเลขเท่านั้น!", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // สร้างฟอร์ม Purchase และส่งข้อมูลไป
            formPurchase purchaseForm = new formPurchase(selectedItems,memberPhone ); // ส่งเฉพาะสินค้าที่เลือกไปยังฟอร์ม Purchase
            purchaseForm.Show();
            this.Hide(); // ซ่อนฟอร์มปัจจุบัน
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void formCart_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }
    }
}