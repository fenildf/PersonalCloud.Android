﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;
using DavideSteduto.FlexibleAdapter.Items;

using Unishare.Apps.DevolMobile.Items;

namespace Unishare.Apps.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.UploadFilesActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class UploadFilesActivity : NavigableActivity, FlexibleAdapter.IOnItemClickListener
    {
        public const string ResultPath = "UploadFilesActivity.File";

        internal cloud_browser R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private DirectoryInfo directory;
        private List<FileSystemInfo> items;

        private int depth;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cloud_browser);
            R = new cloud_browser(this);

            SupportActionBar.Title = "选择文件";

            adapter = new FlexibleAdapter(null, this);
            adapter.SetAnimationOnForwardScrolling(true);
            layoutManager = new SmoothScrollLinearLayoutManager(this);
            R.list_recycler.SetLayoutManager(layoutManager);
            R.list_recycler.SetAdapter(adapter);
            R.list_recycler.AddItemDecoration(new FlexibleItemDecoration(this).WithDefaultDivider());
            R.list_reloader.SetColorSchemeResources(Resource.Color.colorAccent);
            R.list_reloader.Refresh += RefreshDirectory;
            EmptyViewHelper.Create(adapter, R.list_empty);

            directory = new DirectoryInfo(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath);
            RefreshDirectory(this, EventArgs.Empty);
        }

        public bool OnItemClick(View view, int position)
        {
            if (position == 0 && depth != 0)
            {
                directory = directory.Parent;
                depth -= 1;
                RefreshDirectory(this, EventArgs.Empty);
                return false;
            }

            var item = items[depth == 0 ? position : (position - 1)];
            if (item is DirectoryInfo)
            {
                directory = (DirectoryInfo) item;
                depth += 1;
                RefreshDirectory(this, EventArgs.Empty);
            }
            else
            {
                SetResult(Result.Ok, new Intent().PutExtra(ResultPath, item.FullName));
                Finish();
            }

            return false;
        }

        private void RefreshDirectory(object sender, EventArgs e)
        {
            Task.Run(() => {
                try
                {
                    items = directory.EnumerateFileSystemInfos().ToList();
                }
                catch (IOException)
                {
                    items = null;
                    this.ShowAlert("无法查看此文件夹", "此文件夹已被删除或内容异常。");
                }

                var models = new List<IFlexible>();
                if (depth != 0)
                {
                    var parentPath = directory.Parent.Name;
                    models.Add(new FolderGoBack(parentPath));
                }
                models.AddRange(items.Select(x => new FileFolder(x)));
                RunOnUiThread(() => {
                    adapter.UpdateDataSet(models, true);
                    if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
                });
            });
        }
    }
}