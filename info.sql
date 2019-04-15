USE [EOL_FOTON_INFO]
GO

/****** Object:  Table [dbo].[VehicleInfo]    Script Date: 2019/4/10 16:42:09 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[VehicleInfo](
	[ID] [int] IDENTITY(1,1) NOT NULL,       -- ID主键
	[VIN] [varchar](17) NOT NULL,            -- VIN号
	[VehicleType] [varchar](50) NOT NULL,    -- 车型，扫码获取
	[EngineCode] [varchar](50) NOT NULL,     -- 发动机号，扫码获取
	[ProductCode] [varchar](50) NOT NULL,    -- 整车编号，扫码获取
	[LeaveFactoryTime] [datetime] NOT NULL,  -- 出厂时间
	[TestTime] [datetime] NOT NULL,          -- 检测时间
	[TestNumber] [int] NOT NULL,             -- 受检次数
	[TestResult] [int] NOT NULL,             -- 检测结果
	[ABSResult] [varchar](50) NOT NULL,      -- ABS总评结果
	[FWP] [varchar](50) NOT NULL,            -- 四轮驱动工作正常
	[BLOCK] [varchar](50) NOT NULL,          -- 阻滞力合格
	[STEER] [varchar](50) NOT NULL,          -- 转向系正常
	[WIPER] [varchar](50) NOT NULL,          -- 雨刮器齐全有效
	[REARVIEWMIRROR] [varchar](50) NOT NULL, -- 后视镜齐全
	[LIGHT] [varchar](50) NOT NULL,          -- 灯光齐全有效
	[STARTER] [varchar](50) NOT NULL,        -- 起动机发电机运转
	[PIPELINE] [varchar](50) NOT NULL,       -- 管路无漏油
	[WHEELSTANDARD] [varchar](50) NOT NULL,  -- 轮胎符合标志
	[WWSG] [varchar](50) NOT NULL,           -- 风挡风窗使用安全玻璃
	[EMS] [varchar](50) NOT NULL,            -- 发动机管理系统工作正常
	[ENGINE] [varchar](50) NOT NULL,         -- 发动机工作正常
	[TNB] [varchar](50) NOT NULL,            -- 轮胎螺母螺栓坚固可靠
	[RGAFSG] [varchar](50) NOT NULL,         -- 倒挡及前进加速档工作正常
	[TOTSPOR] [varchar](50) NOT NULL,        -- 淋雨密封性能检测
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('') FOR [VIN]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('') FOR [VehicleType]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('') FOR [EngineCode]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('') FOR [ProductCode]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT (getdate()) FOR [LeaveFactoryTime]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT (getdate()) FOR [TestTime]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ((1)) FOR [TestNumber]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ((-1)) FOR [TestResult]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [ABSResult]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [FWP]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [BLOCK]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [STEER]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [WIPER]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [REARVIEWMIRROR]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [LIGHT]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [STARTER]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [PIPELINE]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [WHEELSTANDARD]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [WWSG]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [EMS]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [ENGINE]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [TNB]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [RGAFSG]
GO

ALTER TABLE [dbo].[VehicleInfo] ADD  DEFAULT ('O') FOR [TOTSPOR]
GO
