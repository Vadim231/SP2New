using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
  public partial class Form1 : Form
  {
    public Form1()
    {
      InitializeComponent();
      btnResume.Enabled = false;
    }

    private void btnSourceFile_Click(object sender, EventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.CheckFileExists = true;
      dlg.Multiselect = false;
      dlg.Title = "Моя форма открытия файла";
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        txtSourceFile.Text = dlg.FileName;
      }
    }
    private void btnDestFile_Click(object sender, EventArgs e)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.CheckFileExists = false;
      dlg.OverwritePrompt = true;
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        txtDestFile.Text = dlg.FileName;
      }
      else txtDestFile.Text = "";
    }

    Thread thCopyFile = null;
    CopyParam paramCopyFile = null;

    private void btnStart_Click(object sender, EventArgs e)
    {
      MessageBox.Show("Копирование файла идет не по 4096 кб, а по 200кб. Сделано специально, чтобы можно было успеть поставить процесс на паузу.", "ВНИМАНИЕ", MessageBoxButtons.OK, MessageBoxIcon.Warning);

      if (txtSourceFile.Text.Trim().Length == 0 ||
         txtDestFile.Text.Trim().Length == 0)
      {
        MessageBox.Show("Не указаны рабочие файлы");
        return;
      }
      if (thCopyFile != null)
      {
        return;
      }
      thCopyFile = new Thread(ThCopyRoutine);
      thCopyFile.IsBackground = true;
      paramCopyFile = new CopyParam();

      paramCopyFile.srcFileName =
                      txtSourceFile.Text.Trim();
      paramCopyFile.destFileName =
                      txtDestFile.Text.Trim();

      paramCopyFile.frm = this;
      pbFileCopy.Value = 0;
      pbFileCopy.Minimum = 0;    //   0.0 %
      pbFileCopy.Maximum = 1000; // 100.0 %
      pbFileCopy.Step = 100;     //  10.0 %

      thCopyFile.Start(paramCopyFile);
    }

    int readSize = 200; //для того, чтобы можно было успеть на паузу!

    void ThCopyRoutine(object arg)
    {
      CopyParam par = arg as CopyParam;
      FileStream src =
        new FileStream(par.srcFileName,
                       FileMode.Open,
                       FileAccess.Read);
      FileStream dst = 
        new FileStream(par.destFileName,
                       FileMode.OpenOrCreate,
                       FileAccess.Write);
      // временный буффер для чтения-записи
      byte[] buf = new byte[readSize];

      // Получение длины файла
      FileInfo fi = new FileInfo(par.srcFileName);
      long fileSize = fi.Length;
      long readAll = 0;
      while (!par.IsStop)
      {
        int readBytes = src.Read(buf, 0, readSize);
        dst.Write(buf, 0, readBytes);
        // общее число прочтенных байтов
        readAll += readBytes;
        // 0...1000 ==> 0.0 ... 100.0
        int readProcent =
    (int)((double)readAll / fileSize * 100.0 * 10 + 0.5);
        //par.frm.pbFileCopy.Value = ReadProcent;
        par.frm.pbFileCopy.Invoke(new Action<int>(
          (x) => { par.frm.pbFileCopy.Value = x;
            //par.frm.pbFileCopy.Invalidate();
            par.frm.pbFileCopy.Update();
          }),
          readProcent);

        //if (readBytes < ReadSize) {
        if(readAll == fileSize) {
          par.IsStop = true; //break;
        }
        par.evnPause.WaitOne();
      }
    }

    class CopyParam
    {
      public string srcFileName, destFileName;
      public Form1 frm;
      public bool IsStop;
      public ManualResetEvent evnPause;
      public CopyParam()
      {
        IsStop = false;
        evnPause = new ManualResetEvent(true);
      }
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
      Application.Exit();
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
      btnResume.Enabled = true;
      paramCopyFile.evnPause.Reset();
      paramCopyFile.IsStop = true;
    }

    private void btnResume_Click(object sender, EventArgs e)
    {
      paramCopyFile.evnPause.Set();
      paramCopyFile.IsStop = false;
      btnResume.Enabled = false;
    }
  }
}
