using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace BiliDownload
{
    class Program
    {
        const string GET_VIDEO_LIST_URL = "https://space.bilibili.com/ajax/member/getSubmitVideos";
        const string GET_VIDEO_INFO_URL = "https://api.bilibili.com/x/web-interface/view";
        const string GET_VIDEO_DOENLOAD_URL = "https://api.bilibili.com/x/player/playurl";
        static void Main(string[] args)
        {
            var mid = "393985626";
            Console.Write("input space number:");
            mid = Console.ReadLine();
            Console.WriteLine("===your space number is {0}===", mid);
            Console.WriteLine("请按任意键继续...");
            Console.ReadKey();
            var pageNum = getVideoPageNum(mid);
            Console.WriteLine("===get {0} pages of videos===", pageNum);
            var videoList = getVideoList(mid, (int)pageNum);
            var lenV = videoList.Count;
            for(int i=0;i<lenV; i++)
            {
                var video = getVideoPages(videoList[i]);
                if (video == null) continue;
                var title = video.title;
                var aid = videoList[i];
                var pages = video.pages;
                Console.WriteLine("===downliading video {0} of {1}===", i + 1, lenV);
                for(int j=0;j<pages.Count;j++)
                {
                    var url = getDownloadUrl(aid, pages[j].cid);
                    if (url == null) continue;
                    downloadVideo(aid.Value.ToString(), url[url.Count - 1].baseUrl.Value, title.Value + "-P" + j);
                }
            }
            Console.WriteLine("===all videos downloaded~~~===");
        }
        static void downloadVideo(string aid,string url,string title)
        {            
            var referer = "https://www.bilibili.com/video/av"+aid;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("referer", referer);
            httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0");
            var asyncStatus = httpClient.GetStreamAsync(url);
            while (!asyncStatus.IsCompleted) ;
            FileStream fileStream =  new FileStream(title.Replace("|","") + ".flv", FileMode.Create);
            Stream stream = asyncStatus.Result;
            int tick = 0;
            while (stream.CanRead)
            {
                const int ArrayLength = 102400;
                byte[] array = new byte[ArrayLength];
                int length = stream.Read(array, 0, ArrayLength);
                fileStream.Write(array, 0, length);
                if (length == 0)
                    tick++;
                else
                    tick = 0;
                if (tick > 10) break;
            }
            fileStream.Close();
        }
        static dynamic getDownloadUrl(dynamic aid,dynamic cid,int qn=80)
        {
            HttpClient httpClient = new HttpClient();
            var asyncStatus = httpClient.GetStringAsync(GET_VIDEO_DOENLOAD_URL + string.Format("?" +
                "avid={0}"+
                "&&cid={1}" +
                "&&qn={2}" +
                "&&otype={3}" +
                "&&fnver={4}" +
                "&&fnval={5}", aid,cid,80,"json",0,16));
            while (!asyncStatus.IsCompleted) ;
            var response = JsonConvert.DeserializeObject<dynamic>(asyncStatus.Result);
            if (response.code == 0)
                return response.data.dash.video;
            return null;
        }
        static dynamic getVideoPages(dynamic aid)
        {
            HttpClient httpClient = new HttpClient();
            var asyncStatus = httpClient.GetStringAsync(GET_VIDEO_INFO_URL + string.Format("?" +
                "aid={0}", aid));
            while (!asyncStatus.IsCompleted) ;
            var response= JsonConvert.DeserializeObject<dynamic>(asyncStatus.Result);
            if (response.code.Value == 0)
                return response.data;
            return null;
        }
        static dynamic getVideoPageNum(dynamic mid)
        {
            HttpClient httpClient = new HttpClient();
            var asyncStatus = httpClient.GetStringAsync(GET_VIDEO_LIST_URL + String.Format("?" +
                "mid={0}" +
                "&&pagesize={1}" +
                "&&tid={2}" +
                "&&page={3}" +
                "&&keyword={4}" +
                "&&order=pubdate", mid, 30, 0, 1, ""));
            while (!asyncStatus.IsCompleted) {; }
            var jsonobj = JsonConvert.DeserializeObject<dynamic>(asyncStatus.Result);
            return jsonobj.data.pages;
        }
        static List<dynamic> getVideoList(string mid,int pageNum)
        {
            List<dynamic> list = new List<dynamic>();
            for(int page=1; page < pageNum+1; page++)
            {
                var videos = getVideos(mid, page);
                foreach (var video in videos)
                    list.Add(video);
            }
            return list;
        }
        static List<dynamic> getVideos(string mid,int page)
        {
            List<dynamic> videoList = new List<dynamic>();
            HttpClient httpClient = new HttpClient();
            var asyncStatus = httpClient.GetStringAsync(GET_VIDEO_LIST_URL + String.Format("" +
                "?mid={0}" +
                "&&pagesize={1}" +
                "&&tid={2}" +
                "&&page={3}" +
                "&&keyword={4}" +
                "&&order=pubdate", mid, 30, 0, page, ""));
            while (!asyncStatus.IsCompleted) ;
            var jsonobj = JsonConvert.DeserializeObject<dynamic>(asyncStatus.Result);
            if(jsonobj.status.Value)
            {
                var datas = jsonobj.data.vlist;
                foreach(var data in datas)
                {
                    videoList.Add(data.aid);
                }
            }
            return videoList;
        }
    }
}
