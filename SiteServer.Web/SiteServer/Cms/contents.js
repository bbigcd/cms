﻿var $url = "/pages/cms/contents";

Object.defineProperty(Object.prototype, "getProp", {
  value: function(prop) {
    var key,
      self = this;
    for (key in self) {
      if (key.toLowerCase() == prop.toLowerCase()) {
        return self[key];
      }
    }
  }
});

var data = {
  siteId: parseInt(utils.getQueryString("siteId")),
  channelId: parseInt(utils.getQueryString("channelId")),
  pageLoad: false,
  pageAlert: null,
  pageType: null,
  page: 1,
  pageContents: null,
  count: null,
  pages: null,
  permissions: null,
  columns: null,
  isAllContents: false,
  pageOptions: null,
  isAllChecked: false
};

var methods = {
  getContentUrl: function (content) {
    if (content.checked) {
      return '../redirect.cshtml?siteId=' + content.siteId + '&channelId=' + content.channelId + '&contentId=' + content.id;
    }
    return apiUrl + '/preview/' + content.siteId + '/' + content.channelId + '/' + content.id;
  },

  btnAddClick: function () {
    top.utils.openLayer({
      title: "添加内容",
      url: this.getAddUrl(),
      full: true,
      max: true
    });
  },

  btnEditClick: function(content) {
    top.utils.openLayer({
      title: "编辑内容",
      url: this.getEditUrl(content),
      full: true,
      max: true
    });
  },

  getAddUrl: function() {
    return "cms/editor.cshtml?siteId=" +
    this.siteId +
    "&channelId=" +
    this.channelId +
    "&returnUrl=" +
    encodeURIComponent(location.href);
  },

  getEditUrl: function(content) {
    return (
      "cms/editor.cshtml?siteId=" +
      this.siteId +
      "&channelId=" +
      content.channelId +
      "&contentId=" +
      content.id +
      "&returnUrl=" +
      encodeURIComponent(location.href)
    );
  },

  btnCreateClick: function(e) {
    e.stopPropagation();

    var $this = this;
    this.pageAlert = null;
    if (!this.isContentChecked) return;

    utils.loading(true);
    $api
      .post($url + "/actions/create", {
        siteId: $this.siteId,
        channelContentIds: this.channelContentIds
      })
      .then(function(response) {
        var res = response.data;

        $this.pageAlert = {
          type: "success",
          html:
            "内容已添加至生成列队！<a href='createStatus.cshtml?siteId=" +
            $this.siteId +
            "'>生成进度查看</a>"
        };
      })
      .catch(function(error) {
        $this.pageAlert = utils.getPageAlert(error);
      })
      .then(function() {
        utils.loading(false);
      });
  },

  btnLayerClick: function(options, e) {
    e.stopPropagation();

    this.pageAlert = null;
    var url = "contentsLayer" + options.name + ".cshtml?siteId=" + this.siteId;

    if (options.channelId) {
      url += "&channelId=" + options.channelId;
    } else {
      url += "&channelId=" + this.channelId;
    }
    if (options.contentId) {
      url += "&contentId=" + options.contentId;
    }

    if (options.withContents) {
      if (!this.isContentChecked) return;
      url += "&channelContentIds=" + this.channelContentIdsString;
    }

    if (options.withOptionalContents) {
      if (this.isContentChecked) {
        url += "&channelContentIds=" + this.channelContentIdsString;
      }
    }
    url += "&returnUrl=" + encodeURIComponent(location.href);

    utils.openLayer({
      title: options.title,
      url: url,
      full: options.full,
      width: options.width ? options.width : 700,
      height: options.height ? options.height : 500
    });
  },

  btnContentViewClick: function(contentId, e) {
    e.stopPropagation();

    utils.openLayer({
      title: "查看内容",
      url:
        "contentsLayerView.cshtml?siteId=" +
        this.siteId +
        "&channelId=" +
        this.channelId +
        "&contentId=" +
        contentId,
      full: true
    });
  },

  btnContentStateClick: function(contentId, e) {
    e.stopPropagation();

    utils.openLayer({
      title: "查看审核状态",
      url:
        "contentsLayerState.cshtml?siteId=" +
        this.siteId +
        "&channelId=" +
        this.channelId +
        "&contentId=" +
        contentId,
      full: true
    });
  },

  toggleChecked: function(content) {
    content.isSelected = !content.isSelected;
    if (!content.isSelected) {
      this.isAllChecked = false;
    }
  },

  selectAll: function() {
    this.isAllChecked = !this.isAllChecked;
    for (var i = 0; i < this.pageContents.length; i++) {
      this.pageContents[i].isSelected = this.isAllChecked;
    }
  },

  loadFirstPage: function() {
    if (this.page === 1) return;
    this.loadContents(1);
  },

  loadPrevPage: function() {
    if (this.page - 1 <= 0) return;
    this.loadContents(this.page - 1);
  },

  loadNextPage: function() {
    if (this.page + 1 > this.pages) return;
    this.loadContents(this.page + 1);
  },

  loadLastPage: function() {
    if (this.page + 1 > this.pages) return;
    this.loadContents(this.pages);
  },

  onPageSelect: function(option) {
    this.loadContents(option);
  },

  scrollToTop: function() {
    document.documentElement.scrollTop = document.body.scrollTop = 0;
  },

  getPluginMenuUrl: function(pluginMenu) {
    return pluginMenu.href + "&returnUrl=" + encodeURIComponent(location.href);
  },

  btnPluginMenuClick: function(pluginMenu, e) {
    e.stopPropagation();

    if (pluginMenu.target === "_layer") {
      utils.openLayer({
        title: pluginMenu.text,
        url: this.getPluginMenuUrl(pluginMenu),
        full: true
      });
    }
  },

  loadContents: function(page) {
    var $this = this;

    if ($this.pageLoad) {
      utils.loading(true);
    }

    $api
      .get($url, {
        params: {
          siteId: $this.siteId,
          channelId: $this.channelId,
          page: page
        }
      })
      .then(function(response) {
        var res = response.data;
        var pageContents = [];
        for (var i = 0; i < res.value.length; i++) {
          var content = _.assign({}, res.value[i], {
            isSelected: false
          });
          pageContents.push(content);
        }
        $this.pageContents = pageContents;
        $this.count = res.count;
        $this.pages = res.pages;
        $this.permissions = res.permissions;
        $this.columns = res.columns;
        $this.isAllContents = res.isAllContents;
        $this.page = page;
        $this.pageOptions = [];
        for (var i = 1; i <= $this.pages; i++) {
          $this.pageOptions.push(i);
        }

        if ($this.pageLoad) {
          utils.loading(false);
          $this.scrollToTop();
        } else {
          $this.pageLoad = true;
        }
      })
      .catch(function(error) {
        $this.pageAlert = utils.getPageAlert(error);
      })
      .then(function() {
        utils.loading(false);
      });
  }
};

Vue.component("multiselect", window.VueMultiselect.default);

var $vue = new Vue({
  el: "#main",
  data: data,
  methods: methods,
  computed: {
    isContentChecked: function() {
      if (this.pageContents) {
        for (var i = 0; i < this.pageContents.length; i++) {
          if (this.pageContents[i].isSelected) {
            return true;
          }
        }
      }
      return false;
    },
    channelContentIds: function() {
      var retVal = [];
      if (this.pageContents) {
        for (var i = 0; i < this.pageContents.length; i++) {
          if (this.pageContents[i].isSelected) {
            retVal.push({
              channelId: this.pageContents[i].channelId,
              id: this.pageContents[i].id
            });
          }
        }
      }
      return retVal;
    },
    channelContentIdsString: function() {
      var retVal = [];
      if (this.pageContents) {
        for (var i = 0; i < this.pageContents.length; i++) {
          if (this.pageContents[i].isSelected) {
            retVal.push(
              this.pageContents[i].channelId + "_" + this.pageContents[i].id
            );
          }
        }
      }
      return retVal.join(",");
    }
  },
  created: function() {
    this.loadContents(1);
  }
});
