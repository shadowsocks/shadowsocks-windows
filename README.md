### 带在线获取并同步最新Server参数的功能的一个版本
[![Build Status]][Appveyor]
* 现在由于干扰或一些别的原因，服务器配置会经常修改，而对于多人共享或者其他类似情境，发布和同步频繁修改的的配置就显得比较不便。我加了一个通过url较安全地获取配置的功能，即通过一个发布服务器，客户端可使用rsa加密通信从服务器获取最新的配置并与本地的配置同步，从而实现便捷获取最新配置.
* 其实在之前已经进行过约三个月的小范围测试，累计用户约1400人，效果还算不错，所以在考虑能否加入项目本体.
* 配置文件Server段结构如下

		{
      		"server" : "127.0.0.1",
      		"server_port" : 2333,
      		"password" : "kaguya",
      		"method" : "aes-128-cfb",
      		"remarks" : "新测试1",
      		"auth" : false,
      		"provider" : "新服务器",
      		"fingerprint" : "90a409a19ba0db0f7cd0cff0dffbbcde"
    	},
    	{
      		"server" : "127.0.0.1",
      		"server_port" : 8388,
      		"password" : "test",
      		"method" : "aes-256-cfb",
      		"remarks" : "comment",
      		"auth" : false,
      		"provider" : "local",
      		"fingerprint" : ""
    	}
	与原来相比，增加了provider和fingerprint两项
	
	provider记录了配置的来源，如果是自行手工添加的则为local,若为在线获取的则为发布服务器指定的名称.用于在系统托盘菜单中显示用于指示配置来源.

	fingerprint记录了配置来源服务器的唯一的指纹,若是自行手工添加的则为空.此段用于标记来自同一发布服务器的一组配置的集合，当从同一发布服务器更新配置时，拥有与服务器相同指纹的配置可以被更新/覆盖.

	Server模型和配置文件的其他部分其他均与先前一致.
	
* 工作原理
	* 客户端随机生成一对RSA密钥，并用明文发送公钥至发布服务器.
	* 服务器接收并记录客户端公钥，将服务器的公钥、指纹、随机生成的验证字符串用客户端公钥加密以后发回.
	* 客户端收到服务器公钥和指纹，如果存在对应的本地记录，则将收到的数据与本地记录进行比对，若不相符，判定为无效服务器;若无本地记录，则默认信任服务器有效性并将其加入本地记录.客户端把服务器发来的验证字符串原样返回，同时计算出一个随机字符串，将两者用服务器公钥加密后发往服务器.
	* 服务器收到客户端请求，检查客户端返回的验证字符串并与服务器本地保存的字符串进行对比，若符合则信任客户端身份;否则判定为欺诈请求返回404错误.服务器根据预定策略将Shadowsocks配置和用户生成的验证字符串用客户端公钥加密后返回.
	* 客户端收到数据，校验服务器返回的验证字符串，如果不符，则判定服务器身份不可信;如果相符，信任服务器身份并接受服务器返回的配置，反之则中止该次通信.至此，服务器和客户端完成双向的身份认证和数据的传输.
	* 客户端将本地拥有与服务器相同指纹的配置置用获取的最新位置覆盖，完成配置的更新.

* 操作示例
	* 服务器配置的显示，如下图
		
![default](https://cloud.githubusercontent.com/assets/5331336/11752593/7788730c-a07b-11e5-9a45-b46757475679.png)

	* 在线获取/更新服务器的过程，如下图
![add](https://cloud.githubusercontent.com/assets/5331336/11752616/8b8a28aa-a07b-11e5-9160-1baab4e17826.png)

		1.点击“导入”打开导入配置窗口

		2.输入获得的url，点击“获取”,若无异常，列表框显示获得的配置，若出现异常，弹窗显示异常信息.

		3.选择不想添加的配置，点击“移除选定配置”，排除指定配置.

		4.点击“添加以上配置”，将列表中剩余的配置加入本地配置列表并覆盖同一来源的配置.

* 安全性考虑
	* 这个功能采取“信任新服务器，在本地保存‘服务器-指纹’对作为服务器身份”的策略，当同一指纹在第一次通信安全可靠及服务器私钥未泄露的情况下，可以在后续的更新配置中排除中间人攻击的威胁.
	

*	更多
	* 配套的完整的发布服务器的实现在这里：(基于laravel，有部署脚本)
		[https://github.com/SuperHentai/Shadowsocks-Config-Server](https://github.com/SuperHentai/Shadowsocks-Config-Server)
	* 匆忙之中写完的说明，里面有比较详细的信息:[https://github.com/SuperHentai/Shadowsocks-Config-Server/blob/master/readme.md](https://github.com/SuperHentai/Shadowsocks-Config-Server/blob/master/readme.md)
	[https://github.com/SuperHentai/Shadowsocks-Config-Server/wiki/%E8%AF%B4%E6%98%8E%E6%96%87%E6%A1%A3](https://github.com/SuperHentai/Shadowsocks-Config-Server/wiki/%E8%AF%B4%E6%98%8E%E6%96%87%E6%A1%A3)

	* 放出的测试程序[https://github.com/SuperHentai/shadowsocks-windows/releases/tag/test](https://github.com/SuperHentai/shadowsocks-windows/releases/tag/test)
	* 已部署的测试发布服务器URL:http://test.unwall.org/
	* 发布服务器操作后台[http://test.unwall.org/panel](http://test.unwall.org/panel)
		* 测试用户名：kaguya
		* 测试密码：kaguya

		


[Appveyor]:       https://ci.appveyor.com/project/1136358656/shadowsocks-windows
[Build Status]:   https://ci.appveyor.com/api/projects/status/gkurto00dke10xjl/branch/with_online_config
