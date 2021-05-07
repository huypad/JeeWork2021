// USA
export const locale = {
	lang: 'en',
	data: {
		TRANSLATOR: {
			SELECT: 'Select your language',
		},
		MENU: {
			NEW: 'new',
			ACTIONS: 'Actions',
			CREATE_POST: 'Create New Post',
			PAGES: 'Pages',
			FEATURES: 'Features',
			APPS: 'Apps',
			DASHBOARD: 'Dashboard',
		},
		AUTH: {
			GENERAL: {
				OR: 'Or',
				SUBMIT_BUTTON: 'Submit',
				NO_ACCOUNT: 'Don\'t have an account?',
				SIGNUP_BUTTON: 'Sign Up',
				FORGOT_BUTTON: 'Forgot Password',
				BACK_BUTTON: 'Back',
				PRIVACY: 'Privacy',
				LEGAL: 'Legal',
				CONTACT: 'Contact',
			},
			LOGIN: {
				TITLE: 'Login Account',
				BUTTON: 'Sign In',
				Username: 'Username',
				Password: 'Password',
			},
			FORGOT: {
				TITLE: 'Forgotten Password?',
				DESC: 'Enter your email to reset your password',
				SUCCESS: 'Your account has been successfully reset.'
			},
			REGISTER: {
				TITLE: 'Sign Up',
				DESC: 'Enter your details to create your account',
				SUCCESS: 'Your account has been successfuly registered.'
			},
			INPUT: {
				EMAIL: 'Email',
				FULLNAME: 'Fullname',
				PASSWORD: 'Password',
				CONFIRM_PASSWORD: 'Confirm Password',
				USERNAME: 'Username'
			},
			VALIDATION: {
				INVALID: '{{name}} is not valid',
				REQUIRED: '{{name}} is required',
				MIN_LENGTH: '{{name}} minimum length is {{min}}',
				AGREEMENT_REQUIRED: 'Accepting terms & conditions are required',
				NOT_FOUND: 'The requested {{name}} is not found',
				INVALID_LOGIN: 'The login detail is incorrect',
				REQUIRED_FIELD: 'Required field',
				MIN_LENGTH_FIELD: 'Minimum field length:',
				MAX_LENGTH_FIELD: 'Maximum field length:',
				INVALID_FIELD: 'Field is not valid',
			}
		},
		ECOMMERCE: {
			COMMON: {
				SELECTED_RECORDS_COUNT: 'Selected records count: ',
				ALL: 'All',
				SUSPENDED: 'Suspended',
				ACTIVE: 'Active',
				FILTER: 'Filter',
				BY_STATUS: 'by Status',
				BY_TYPE: 'by Type',
				BUSINESS: 'Business',
				INDIVIDUAL: 'Individual',
				SEARCH: 'Search',
				IN_ALL_FIELDS: 'in all fields'
			},
			ECOMMERCE: 'eCommerce',
			CUSTOMERS: {
				CUSTOMERS: 'Customers',
				CUSTOMERS_LIST: 'Customers list',
				NEW_CUSTOMER: 'New Customer',
				DELETE_CUSTOMER_SIMPLE: {
					TITLE: 'Customer Delete',
					DESCRIPTION: 'Are you sure to permanently delete this customer?',
					WAIT_DESCRIPTION: 'Customer is deleting...',
					MESSAGE: 'Customer has been deleted'
				},
				DELETE_CUSTOMER_MULTY: {
					TITLE: 'Customers Delete',
					DESCRIPTION: 'Are you sure to permanently delete selected customers?',
					WAIT_DESCRIPTION: 'Customers are deleting...',
					MESSAGE: 'Selected customers have been deleted'
				},
				UPDATE_STATUS: {
					TITLE: 'Status has been updated for selected customers',
					MESSAGE: 'Selected customers status have successfully been updated'
				},
				EDIT: {
					UPDATE_MESSAGE: 'Customer has been updated',
					ADD_MESSAGE: 'Customer has been created'
				}
			}
		},
		// department:{
		// 	list: 'List',
		// 	tieude1: 'Danh sách phòng ban',
		// 	timkiemnhanh: 'Lọc nhanh department',
		// 	themmoi: 'Tạo mới department',
		// 	taomoi: 'Thêm mới phòng ban',
		// 	chinhsua: 'Chỉnh sửa phòng ban',
		// 	ten: 'Dept name',
		// 	chusohuu: 'Dept name',
		// 	motachusohuu: 'Dept owners can manage milestones'
		// },
		// từ đây xuống
		mainMenuJeeHR: {
			//---- phần này dành cho main menu ----------
			quanlycongviec: 'Workflow management',
			setupmenu: 'Setup menu',
			wework: 'Work connection',
			adminworkflow: 'Admin',
			quantrong: 'Important',
			quanlytaikhoan: 'Account management',
			quanlydanhmuc: 'Category management',
			canhan: 'Personal',
			quanly: 'Manager',
			quanlytaisanxe: 'Property management - vehicles', // Quản lý tài sản - phương tiện
		},
		subMenuJeeHR: {
			// Thống nhất đặt tên cho resource menu: Viết liền không dấu, mọi ký tự đều viết thường.
			//=============Workflow====================
			quytrinhdong: 'Set up dynamic processes',
			danhmucdoituong: 'List of objects',
			taocongviec: 'Create quests', // tạo nhiệm vụ
			nhiemvu: 'My quests',// my duty
			congviec: 'My work',
			danhmucloaiquytrinh: 'List of process types',
			//=============We work====================
			listdepartment: 'List department',
			listproject: 'Project and department',
			phanquyentaikhoan: 'Account decentralization',
			tasks: 'Tasks', // công việc
			users: 'Users',
			reports: 'Reports',
			permision: 'Permission',//Phân quyền chức năng
			wedepartment: 'Department management',//Quản lý phòng ban
			capnhatdepartment: 'Update department',
			viewreportdepartment: 'View report department',
			quanlyduanphongban: 'Project / departmental management',
			capnhatprojectteam: 'Update project team',
			viewactivities: 'View activities project team',
			viewmilestone: 'View milestone project team',
			wemember: 'Manage other members',
			wereport: 'Report work',
			wepermision: 'Permission',//Phân quyền chức năng
			thaoluan: 'View and discuss in the topic of the project team involved',//Xem và thảo luận trong topic của dự án/phòng ban tham gia
			
			//=============Quản lý tài sản===============
			dangkyxe: 'Đăng ký xe',
			yeucaukhaibaots: 'Yêu cầu cấp phát tài sản',
		},
		phanquyen: {
			tieude1: 'Manage user groups',//Quản lý nhóm người dùng
			tieude2: 'Manage user',//Quản lý người dùng
			nhomnguoidung: 'User groups',
			tennhom: 'Group name',
			themnhom: 'Add group',
			nguoidung: 'Users',
			hoten: 'Fullname',
			manv: 'Code',
			chucdanh: 'Chức danh',
			chucvu: 'Position',//Chức vụ
			donvi: 'Unit',//Đơn vị
			phongban: 'Department',//Phòng ban
			tatca: 'All',//Tất cả
			tendangnhap: 'User name',
			timkiem: 'Search by full name',//Tìm kiếm theo họ tên
			phanquyen: 'Decentralization',//Phân quyền
			danhsachnguoidung: 'List of group users',//Danh sách người dùng nhóm
			khoa: 'Lock',
			phanquyennhomnguoidung: 'User group decentralization',//Phân quyền nhóm người dùng
			hanquyennguoidung: 'Phân quyền người dùng',
			quyen: 'Permission',//Quyền
			danhsachnguoidunghethong: 'Danh sách người dùng hệ thống',//Danh sách người dùng hệ thống
			danhsachnguoidungnhom: 'List of group users',
			xem: 'Only view',
			sua: 'Repair',
			themnguoidungvaonhom: 'Add user to the group',//Thêm người dùng vào nhóm
			phanquyenchucnang: 'Phân quyền chức năng',//note
			phanquyenthietlap: 'Decentralized setting',//Phân quyền thiết lập
			ID: 'ID',
			id_nhom:'Id group'
		},
		JeeHR: {
			boqua: 'Next', // bỏ qua
			cancel: 'Cancel',
			bancochacchanmuonxoakhong: 'Are you sure want to delete thiss data',//Bạn có chắc muốn xóa dữ liệu này không?
			// C
			cactuychonkhac: 'Orther option',
			caidat: 'Setting',
			capnhat: 'Update',
			capnhatthanhcong: 'Update success',
			capnhatthatbai: 'Update failed',
			chinhsua: 'Edit',
			changetype: 'Change type project/Team',
			chinhsuanhanh: 'Quick editing',
			chinhsuacongviec: 'Edit work',
			capnhattrangthai: 'Update status',
			chitiet: "View detail",
			choduyet: 'Pending',//Chờ duyệt
			chon: 'Choose',//Chọn
			choncot: 'Select column',//Chọn cột
			choncocautochuc: 'Choose your organization structure',//Chọn cơ cấu tổ chức
			chonhinhthuc: 'Choose your form',//Chọn hình thức
			chonloai: 'Select type',//Chọn loại
			chonloaihopdong: 'Choose a contract type',
			chonloainhanvien: 'Chọn loại nhân viên',//--
			chonloaiphep: 'Chọn loại phép',//--
			chonngonngu: 'Choose your language',
			chonnhanvien: 'Chọn nhân viên',//--
			chonphucap: 'Chọn phụ cấp',//--
			chonquanhenhanthan: 'Chọn quan hệ nhân thân',//--
			chonquanhuyen: 'Chọn quận/huyện',//--
			chontatca: 'Select all',// chọn tất cả
			chonthang: 'Chọn tháng',//--
			chontinhtp: 'Chọn tỉnh/TP',//--
			chontinhtrang: 'Chọn tình trạng',//--
			chonphanloai: 'Chọn phân loại',//--
			chongiaidoan: 'Chọn giai đoạn',//--
			chucdanh: 'Chức danh',//--
			chuyengiaidoan: 'Chuyển giai đoạn',//---
			chucvu: 'Chức vụ',//--
			chuagui: 'Chưa gửi',//--
			cmnd: 'Chứng minh nhân dân',//--
			congviec: 'Công việc',
			cocautochuc: 'Phòng ban/bộ phận',//--
			chonchucdanh: 'Select title jobs',//--
			congdu: 'Công đủ',//--
			cvlaplai: 'Reepeat Task',// Công việc lặp lại
			chuamotacongviec: 'Chưa có mô tả cho công việc này',//--
			khongcotailieudinhkem: 'Không có tài liệu đính kèm',//--
			tepdinhkem: 'Tệp đính kèm',//--
			// D
			duyet: 'Duyệt', //--
			daduyet: 'Approved',// Đã duyệt
			dangxuat: 'Log out',
			dangnhap: 'Log in',
			dongteam: 'Close team',
			dulieudangduocxoa: 'Data is being deleted',
			dong: 'Close',
			danhsachthaoluan: 'Discussions',
			department: 'Department',
			// G
			gui: 'Send',
			giaoviec: 'Assign',
			// H
			hoantatthietlap: 'Complete setup',
			display: 'Display',
			filterproject: 'Quick filter projects/teams',
			hoatdongganday: 'Recent activity',
			huy: 'Cancel',
			// K
			keyword: 'Key work',
			khoangthoigian: 'Peried',
			khongcodulieu: 'No data',
			khongduocduyet: 'Not approved', // không được duyệt
			KhongDuyet: 'Not approved',//không duyệt
			khongduyetthanhcong: 'Không duyệt thành công', //
			khongtimthaynguoiduyet: 'Không tìm thấy người duyệt',
			khoancongtruluong: 'Khoản cộng trừ lương',
			khongduyet: 'Không duyệt',
			khongxacnhan: "Không xác nhận",
			khoiduan: "Khỏi dự án",
			// L
			Lan1: 'First',//Lần 1
			Lan2: 'Second',
			Lan3: 'Third',
			luu: 'Save',
			luudong: 'Save & close',
			luutieptuc: 'Save & continue',
			luuvagui: "Save & send notifications",
			luuthietlap: 'Save & set up',
			luuvaohoanthanh: 'Save completed',
			// M
			manv: 'Code',
			matkhau: 'Password',
			MauIn: 'Chọn mẫu in',
			mapping: 'Execute',
			mota: 'Description',
			moteam: 'Open team',
			// N
			nhanbanteam: 'Replication team',
			no: 'No',
			ngaycapnhat: 'Update day',
			nguoicapnhat: 'Update person',// người cập nhật
			ngaybatdau: 'Start day',
			ngayketthuc: 'End day',
			ngay: 'Day',
			notassigned: 'Not assigned',
			// P
			phailaso: ' is number',
			phongban: 'Department',
			pic: 'Pic',
			stage: 'Filter stage',
			// Q
			quanlyduan: 'Project management',
			// S
			stt: 'No',
			report: 'Report',
			// T
			tacvu: 'Action',// tacs vu
			taifile: 'Download file',
			TangCa: 'T.Ca',
			TangCaDem: 'T.Ca đêm',
			taohopdongthanhcong: 'Tạo hợp đồng thành công',
			taothuyenchuyen: 'Tạo thuyên chuyển',
			tatca: 'All',
			tenchucvu: 'Tên chức vụ',
			thanhcong: 'Success',
			themdong: 'Add & close',
			themmoi: 'Add new',
			them: 'Add',
			themnhieuthanhvien: 'Add more members',
			themnhieuquanlyduan: 'Add more project management',
			themthanhvien: 'Add member',
			themquyen: 'Add permissions',
			themquanlyduan: 'Add project management',
			themthanhcong: 'Add success',//Thêm thành công
			themthatbai: 'Add failed',//Thêm thất bại
			themtieptuc: 'Add & continue',//Thêm & tiếp tục
			thietlap: 'Setup',//Thiết lập
			thoiviec: 'Thôi việc',
			thoiviectungay: 'Thôi việc từ ngày',
			thongbaoquyen: 'Không tìm thấy thông tin nhân viên',
			thongke: 'View statistics',// xem thoongs ke
			tieude: 'Title',
			tim: 'Search',//Tìm
			time: 'Enter the time',//Nhập thời gian
			timduoc: 'Tìm được',
			timkiem: 'Search',
			timkiem1: 'Search by code and full name',
			timkiemnhanvien: 'Search employees',
			tinhtrang: 'Status',//tình trạng
			tieptuc: 'Continue',
			trolai: 'Back',
			tralai: 'Return', // trả lại
			tukhoa: 'Key work',
			thanhvien: 'Member',
			trangthai: 'Status',
			taixuong: 'Download',
			// U
			upload: 'Upload file',
			// V

			// X
			xacnhan: "Confirm",
			xem: 'View',
			xemtruoc: 'Preview',
			xemdanhsach: 'View list',
			xoa: 'Delete',
			xoafile: 'Delete File',
			xoateam: 'Delete team',
			xoatatca: 'Delete all',
			xoathanhcong: 'Deleted successfully',
			xoathatbai: 'Deleted failed',
			xuatexcel: 'Export excel',
			xuatword: 'Export word',
			xemdulieu: 'View data',
			xemhuongdan: 'Xem hướng dẫn sử dụng',
			xemdanggird: 'View in grid',
			xemdanglist: 'View in list form',
			// Y
			yes: 'Yes',
			//====================Dùng cho phần xác nhận duyệt - không duyệt (Phép, tăng ca, .....)
			nhanvien: 'Staff',
		},
		//===================Dung cho phần workfollow===================
		process: {
			tieude1: 'Quy trình động',
			tenquytrinh: 'Tên quy trình',
			doituong: 'Đối tượng',
			themmoi: 'Thêm mới quy trình động',
			chinhsua: 'Cập nhật quy trình động',
			apdung: 'Áp dụng cho',
			theodoi: 'Người theo dõi',
			vequytrinh: 'Vẽ quy trình',
			tencongviec: 'Tiêu đề',
			thoigian: 'Time',
			donvi: 'Đơn vị',
			nguoigiaoviec: 'Người quản lý (Phòng ban, vị trí, người dùng)',
			nguoitheodoi: 'Người theo dõi (Phòng ban, vị trí, người dùng)',
			gio: 'Giờ',
			ngay: 'Ngày',
			mota: 'Mô tả',
			khonggioihan: 'Không giới hạn thời gian',
			quytrinh: 'Quy trình',
			hoantat: 'Hoàn tất thiết lập',
			truongtuychinh: 'Trường tùy chỉnh',
			themtruongdulieutuychinh: 'Thêm trường dữ liệu tùy chỉnh',
			capnhattruongdulieutuychinh: 'Cập nhật trường dữ liệu tùy chỉnh',
			chontruongdulieutuychinh: 'Chọn trường dữ liệu tùy chỉnh',
			tieude2: "Danh sách trường dữ liệu tùy chỉnh",
			//=====================Chỉnh sửa ver1=====================
			congviec: 'CÔNG VIỆC',
			tengiaidoan: 'Tên giai đoạn',
			thoigianhoanthanh: 'Thời gian hoàn thành',
			soluong: 'Số lượng',
			chophepchinhsuadeadline: 'Cho phép chỉnh sửa deadline',
			batbuochoanthanh: 'Bắt buộc hoàn thành tất cả các công việc chi tiết',
			nguoiquanlygiaidoan: 'Người quản lý giai đoạn',
			nguoithuchien: 'Người thực hiện',
			cachgiaoviec: 'Cách giao việc',
			thongtingiaidoan: 'Thông tin giai đoạn',
			congviecchitiet: 'Công việc chi tiết',
			thongtincannhap: 'Thông tin cần nhập',
			khong: 'Không',
			co: 'Có',
			themmoicongviec: 'Thêm mới công việc chi tiết',
			chinhsuacongviec: 'Chỉnh sửa công việc chi tiết',
			quyetdinh: 'QUYẾT ĐỊNH',
			mauthongtinxemquyetdinh: 'Mẫu thông tin để xem và quyết định',
			batdau: 'BẮT ĐẦU',
			cachbatdauquytrinh: 'Cách bắt đầu quy trình',
			chondoituong: 'Chọn đối tượng',
			loainhanvien: 'Loại nhân viên',
			sukien: 'Sự kiện',
			nguoidungbatdau: 'Người dùng bắt đầu',
			lienket: 'Liên kết từ đối tượng khác',
			operator: 'Operator',
			giatri: 'Giá trị',
			thongtinmauemail: 'Thông tin mẫu email',
			themtruongtuychinh: 'Thêm trường tùy chỉnh',
			guimail: 'GỬI MAIL',
			//====================Quy trình con=============
			thongtinyeucau: 'Thông tin yêu cầu',
			dulieuchuyentiep: 'Dữ liệu chuyển tiếp',
			thietlapchuyentiep: 'Thiết lập chuyển tiếp thông tin',
			quytrinhkhac: 'Quy trình khác',
			themmoichuyentiep: 'Thêm mới chuyển tiếp thông tin',
			chinhsuachuyentiep: 'Chỉnh sửa chuyển tiếp thông tin',
			loaiquytrinh: 'Loại quy trình',
			//===============================================
			quantri: 'Thành viên quản trị quy trình',
			nhomthanhvien: 'Nhóm thành viên có quyền tạo nhiệm vụ mới',
			tuychonxem: 'Tùy chọn xem nhiệm vụ',
			tuychonxem1: 'Chỉ cho những thành viên theo dõi',
			tuychonxem2: 'Tất cả thành viên',
			nhomthanhvienxemquytrinh: 'Nhóm thành viên có thể xem quy trình',
			nhaplydothatbai: 'Nhập một lý do thất bại có thể có',
			nhaplydohoanthanh: 'Nhập một lý do hoàn thành có thể có',
		},
		object: {
			tieude1: 'List of objects',
			tendoituong: 'Name of the object',
			loaidoituong: 'Object type',
			mota: 'Description',
			doituongcosan: 'Objects available',
			doituongtudinhnghia: 'Self-defined object',
			themmoi: 'Add new objects',
			chinhsua: 'Update object',
			themmoi1: 'Add new data field',
			chinhsua1: 'Update the data field',
			thietlapdoituong: 'Set the data field for the object',
			tentruongdulieu: 'Name of the data field',
			loaidulieu: 'Data type',
			truongbatbuoc: 'Required field',
			nhapgiatri: 'Enter the value',
			tukhoa: 'Key work',
		},
		//===================================WorkFlow====================================
		wf: {
			capnhatketquacongviec: 'Cập nhật kết quả công việc',
			tencongviec: 'Tên công việc',
			motacongviec: 'Mô tả công việc',
		},
		workprocess: {
			tieude: "Mission list",//Danh sách nhiệm vụ
			chitiet: 'Mission detail',//Chi tiết nhiệm vụ
			taocongviec: 'Create a new task',//Tạo công việc mới
			tencongviec: 'Task name',
			tenquytrinh: 'Process name',
			mota: 'Description',
			quytrinh: 'Process',
			nguoithuchien: 'Người thực hiện',
			nguoitheodoi: 'Người theo dõi',
			taocongviecthanhcong: 'Tạo nhiệm vụ thành công',
			thongtinquytrinh: 'THÔNG TIN QUY TRÌNH',
			tongthoigian: 'TỔNG THỜI GIAN',
			dadung: 'Đã dùng',
			batdau: 'Bắt đầu',
			yeucau: 'Yêu cầu',
			khonggioihan: 'Không giới hạn',
			lichsuthaotacchinh: 'LỊCH SỬ THAO TÁC CHÍNH',
			dealine: 'Hạn chót',
			dabatdau: 'Đã bắt đầu',
			dalam: 'Đã làm',
			nguoiquanlygiaidoan: 'Người quản lý giai đoạn',
			quahan: 'OVERDUE',
			thongtinchitiet: 'Thông tin chi tiết',
			xemtatca: 'Xem tất cả',
			xemtruongcuagiaidoan: 'Xem trường của giai đoạn',
			chuyengiaidoan: 'Chuyển giai đoạn',
			chuathuchien: 'Chưa thực hiện',
			tamdung: 'Tạm dừng',
			thuchien: 'Thực hiện',
			hoanthanh: 'Completed',
			capnhattinhtrang: 'Cập nhật tình trạng giai đoạn',
			giaolaichonguoikhac: 'Giao lại cho người khác',
			capnhatgiaidoan: 'Cập nhật giai đoạn',
			bancochacchanmuonthuchien: 'Bạn có chắc chắn muốn thực hiện giai đoạn?',
			bancochacchanmuontamdung: 'Bạn có chắc chắn muốn tạm dừng giai đoạn?',
			bancochacchanmuonhoanthanh: 'Bạn có chắc chắn muốn hoàn thành giai đoạn?',
			dulieudangduocxyly: 'Dữ liệu đang được xử lý',
			lydo: 'Lý do',
			chuyengiaidoanthanhcong: 'Chuyển giai đoạn thành công',
			capnhathanchot: 'Cập nhật hạn chót',
			giaonguoithuchien: 'Giao người thực hiện',
			themnguoitheodoi: 'Thêm người theo dõi quy trình',
			xoanguoitheodoi: 'Xóa người theo dõi',
			capnhatcongviec: 'Cập nhật công việc',
			bancochacchanmuonthuchiencongviec: 'Bạn có chắc chắn muốn thực hiện công việc?',
			bancochacchanmuontamdungcongviec: 'Bạn có chắc chắn muốn tạm dừng công việc?',
			bancochacchanmuonhoanthanhcongviec: 'Bạn có chắc chắn muốn hoàn thành công việc?',
			taonhiemvu: 'Tạo nhiệm vụ mới',
			tennhiemvu: 'Tên nhiệm vụ',
			chitietquytrinhcon: 'Chi tiết quy trình con',
			chinhsuagiaidoan: 'Chỉnh sửa giai đoạn',
			mauquyetdinh: 'Mẫu quyết định',
			chonketqua: 'Chọn kết quả',
			manhinhkanban: 'Home',
			manhinhdanhsach: 'Nhiệm vụ',
			giao: 'Giao',
			timkiemtheotennhiemvu: 'Tìm kiếm theo tên nhiệm vụ',
			tennguoitao: 'Người tạo',
			tennguoithuchien: 'Người thực hiện',
			vaitro: 'Vai trò',
			hanchot: 'Hạn chót',
			tao: 'Tạo',
			nhanbannhiemvu: 'Nhân bản nhiệm vụ',
			capnhatnhiemvu: 'Cập nhật nhiệm vụ',
			bancochacchanmuonthuchiennhiemvu: 'Bạn có chắc chắn muốn thực hiện nhiệm vụ?',
			bancochacchanmuontamdungnhiemvu: 'Bạn có chắc chắn muốn tạm dừng nhiệm vụ?',
			bancochacchanmuonhoanthanhnhiemvu: 'Bạn có chắc chắn muốn hoàn thành nhiệm vụ?',
			khoaquytrinh: 'Khóa quy trình',
			khoathongbao: '	Bạn có chắc mình muốn khóa quy trình công việc này? Mọi người sẽ không thể tạo thêm các nhiệm vụ mới, nhưng các nhiệm vụ trong quy trình bị đóng vẫn có thể được tiếp tục thực hiện bình thường',
			dulieudangduocxuly: 'Dữ liệu đanh được xử lý',
			moquytrinh: 'Mở quy trình',
			mothongbao: 'Bạn có chắc mình muốn mở lại quy trình công việc này? Mọi người sẽ có thể xem và tạo thêm các nhiệm vụ mới',
			//====================Tab nhiệm vụ====================
			nhiemvu: 'Nhiệm vụ',
			giaidoan: 'Giai đoạn',
			trangthai: 'Trạng thái',
			giaocho: 'Assign to',
			conlai: 'Còn lại',
			congviec: 'Công việc',
			taoboi: 'Created by',
			luc: 'at',
			nhan: 'Nhận',
			thoigian: 'Time',
			//====================Tab hoạt động====================
			hoatdong: 'Activities',
			loailydo: 'Loại lý do',
			lydohoanthanh: 'Chọn lý do đánh dấu hoàn thành nhiệm vụ',
			lydothatbai: 'Chọn lý do đánh dấu thất bại nhiệm vụ',
			xoaquytrinh: 'Xóa quy trình',
			xoathongbao: 'Bạn có chắc mình muốn xóa quy trình công việc này? Tất cả các nhiệm vụ liên quan của tất cả mọi người khác cũng sẽ bị xóa và không thể khôi phục được.',
			//===================Tab thành viên====================
			thanhvien: 'Thành viên',
		},
		//===================================Màn hình nhiệm vụ==============================
		nhiemvu: {
			danhsachnhiemvu: 'Danh sách nhiệm vụ',
			nhiemvuduocgiao: 'Nhiệm vụ được giao',
			nhiemvudatao: 'Nhiệm vụ đã tạo',
			dangtheodoi: 'Đang theo dõi',
			nhanviencapduoi: 'Nhân viên cấp dưới',
			timkiem: 'Tìm theo tên nhiệm vụ, giai đoạn, quy trình',
			dangxuly: 'ĐANG XỬ LÝ',
			quahan: 'OVERDUE',
			hoanthanh: 'Completed',
			tatca: 'TẤT CẢ',
			nguoithuchien: 'Người thực hiện',
			congviec: 'Công việc',
			nhan: 'Nhận',
			deadline: 'Hạn chót',
			done: 'Done',
			tennhiemvu: 'Tên nhiệm vụ',
			tengiaidoan: 'Tên giai đoạn',
			tenquytrinh: 'Tên quy trình',
			mieuta: 'MIÊU TẢ',
			mauquyetdinh: 'Mẫu quyết định',
			chuathuchien: 'Chưa thực hiện',
			tamdung: 'Đang tạm dừng',
			thuchien: 'Đang thực hiện',
			quahan1: 'OVERDUE',
			chonketqua: 'Chọn kết quả',
			batdau: 'Bắt đầu',
		},
		congviec: {
			danhsachcongviec: 'Danh sách công việc',
			congviecduocgiao: 'Công việc được giao',
			congviecdatao: 'Công việc đã tạo',
			congvieccuacapduoi: 'Công việc của cấp dưới',
			danglam: 'ĐANG LÀM',
			quahan: 'OVERDUE',
			hoanthanh: 'Completed',
			tatca: 'TẤT CẢ',
			nguoithuchien: 'NGƯỜI THỰC HIỆN',
			tacvuliemket: 'Tác vụ liên kết',
			nhan: 'NHẬN',
			deadline: 'DEADLINE',
			done: 'DONE',
			tacvulienket: 'Tác vụ liên kết',
			capnhatketqua: 'Cập nhật kết quả công việc',
			timkiem: 'Tìm theo tên công việc, nhiệm vụ, giai đoạn, quy trình',
			tennhiemvu: 'Tên nhiệm vụ',
			tengiaidoan: 'Tên giai đoạn',
			tenquytrinh: 'Tên quy trình',
			tencongviec: 'Tên công việc',
			hoanthanhthucte: 'Hoàn thành thực tế',
			ketquacongviec: 'Kết quả công việc',
			motacongviec: 'Mô tả công việc',
			muctieu: 'Mục tiêu',
			_deadline: 'Completed',
			ngaybatdau: 'Ngày bắt đầu',
			ngayketthuc: 'Ngày kết thúc',
			nguoigiao: 'Người giao',
			nguoitheodoi: 'Người theo dõi',
			_nguoithuchien: 'Người thực hiện',
			tags: 'Tags',
			trangthai: 'Trạng thái',
			milestone: 'Milestone',
			uutien: 'Ưu tiên (urgent)',
			loi: 'Lỗi',
			tensheet: 'Tên sheet',
			stt: 'STT',
			import: 'Import',
		},
		dynamicform: {
			cachgiaoviec: 'Cách giao việc',
			deadline: 'Deadline',
			tgyeucau: 'Yêu cầu',
			nguoithuchien: 'Người thực hiện',
		},
		kanban: {
			tieude1: 'Danh sách quy trình',
			tieude2: 'Chi tiết quy trình',
			quanlytuychonkeonguoc: 'Quản lý tùy chọn kéo ngược',
			tuychonkeonguoc: 'Tùy chọn kéo ngược',
			tuychonkeonguoc1: 'Không thê di chuyển ngược',
			tuychonkeonguoc2: 'Chỉ được di chuyển về giai đoạn gần nhất',
			tuychonkeonguoc3: 'Di chuyển về giai đoạn cụ thể',
			danhsachgiaidoan: 'Danh sách giai đoạn',
			chonquytrinh: 'Chọn quy trình',
		},
		loaiquytrinh: {
			tieude: 'Danh sách loại quy trình',
			tenloaiquytrinh: 'Tên loại quy trình',
			mota: 'Mô tả',
		},
		department: {
			list: 'List',
			tieude1: 'List department',
			timkiemnhanh: 'Quick filter department',
			themmoi: 'Create department',
			taomoi: 'Create department',//Thêm mới ban
			chinhsua: 'Edit department',//Chỉnh sửa ban
			ten: 'Deparment name',
			chusohuu: 'Owner',
			motachusohuu: 'Dept owners can manage milestones',
			project: 'Project team',//Dự án và phòng ban
			department: 'Department',
			thanhvien: 'Member',
			thongke: 'Statistical',//Thống kê
			xemchitietduan: 'View detail projectteam',//Xem chi tiết dự án phòng ban
			muctieu: 'Milestone',//Mục tiêu
			tabchinhsua: 'Edit',//Chỉnh sửa
			chitietmuctieu: 'Mileston detail',//Chi tiết mục tiêu
			chinhsuamuctieu: 'Edit milestone',//Chỉnh sửa mục tiêu
			tenmuctieu: 'Milestone name',
			milestonedate: 'Deadline milestone',
			confirmxoa: 'Confirm to delete department? Only department without projects and milestones could be removed.',
			createmilestone: 'Create milestone'
		},
		departmentandproject: {
			themtasklistmoi: 'Create a new taskList',// Thêm task list mới
			themstatusmoi: 'Create a new status',// Thêm task list mới
			xuatcvrafileexcel: 'Export work to Excel file',
			nhapcvtufileexcel: 'Import work from Excel file',
			emailthongbao: 'Email notifications',
			tiendo:'Track',//-- 
			dungtiendo: 'On track',
			chamtiendo: 'Off track',
			ruirocao: 'At risk',
			baogom: 'Include',
			canluuy: 'Need attentions',
			chinhsuamuctieu: 'Edit milestone',
			chinhsuaduan: 'Edit project',
			chitietmuctieu: 'Milestone detail',
			chuabatdau: 'Not yet started',//Chưa bắt đầu
			chuyenloaiteam: 'Change type team',//Chuyển loại team
			close_status: 'Closed status',
			open_status: 'Open status',
			status: 'Status',
			cocheckists: 'Yes,keep the ckecklist',// có giữ lại check list
			cocongvieccon: 'Yes,keep your job',
			cogiulaitruongdulieutuychinh: 'Yes,keep the custom field',//Có, giữ lại trường dữ liệu tùy chỉnh
			confirmchange: 'Would you like switch this type of project team?',//Bạn có muốn chuyển loại dự án/phòng ban này không?
			confirmxoa: 'If you decide and confirm to delete, the data (and several linked data) WILL BE REMOVED AND WILL NOT be recoverable.',
			//confirmxoa: 'Bạn có chắc muốn xoá dự án này? Các công việc/nhóm công việc liên quan cũng sẽ bị xoá và KHÔNG THỂ khôi phục.',
			congvieccuatoi: 'My task',
			congviecdangdanhbang: 'Board view',
			congviecdangdanhsach: 'List view',
			congviecdangperiod: 'Period view',
			congviecdangstream: 'Stream view',
			covagiulaihanhoanthanh: 'Yes,retaining and changin deadline?',//Có, giữ lại và thay đổi thông tin hạn hoàn thành
			department: 'Department',
			dieuchinhhanhoanthanh: 'Update deadline(Hours)',//Điều chỉnh hạn hoàn thành (Theo giờ)
			discuss: 'Discussions',
			dokhancap: 'Urgency',
			duanhuy: 'Cancel project',//Dự án hủy
			duankhachhang: 'The project only works with the client.',//Dự án làm việc riêng với khách hàng
			duannoibo: 'Interal projects',//Dự án nội bộ
			duanphongban: 'Project team',//Dự án/phòng ban
			duanthanhcong: 'Successful project',
			duanthatbai: 'The project failed',
			dulieudangduocthaydoi: 'Data is being change',
			gannhan: 'Select tag',
			gantt: 'Tasks (Gantt)',
			giaodi: 'Delivered and overdue',//Giao đi và quá hạn
			giulaichecklists: 'Keep a checklist',//Giữ lại checklists
			giulaicongvieccon: 'Giữ lại công việc con',//--
			giulaithongtinhanhoanthanh: 'Keep deadline infomation',
			giulaithongtinnguoigiao: 'Giữ lại thông tin người giao',//--
			giulaithongtinnguoinhan: 'Giữ lại thông tin người nhận',
			giulaithongtinnguoitheodoi: 'Keep follower infomation',
			giulaitruongdulieutuychinh: 'Keep custom data fields',
			giuthongtinhanhoanthanh: 'Có, giữ nguyên thông tin hạn hoàn thành',
			giuthongtinnguoigiao: 'Có, giữ nguyên thông tin người giao việc',
			giuthongtinnguoinhan: 'Giữ lại thông tin người nhận việc',
			giuthongtinnguoitheodoi: 'Có, giữ lại tất cả người theo dõi công việc',
			hoatdong: 'Activities',
			khongchecklist: 'No, delete all checklists',
			khongcongvieccon: 'Không, xóa hết các công việc con',
			khonggiulaitruongdulieutuychinh: 'No, delete custom data field',
			khonggiuthongtinnguoigiao: 'Không, thiết lập TÔI là người giao việc',
			khonggiuthongtinnguoinhan: 'Không, bỏ trống thông tin người nhận việc',
			khonggiuthongtinnguoitheodoi: 'Không, bỏ trống thông tin người theo dõi',
			khongnhanban: 'No duplicate',//Không nhân bản
			khongthietlaphanhoanthanh: 'No, no deadline is set for work',
			lichbieu: 'Calendar',
			loaiduan: 'Project type',
			maudactrung: 'Màu đặc trưng',
			milestone: 'Milestone',
			milestonedate: 'Deadline milestone',
			moiduocgiao: 'Recently assigned',
			motangangonveduan: 'Brief description of the project',// Mô tả ngắn gọn về dự án
			motangangonvephongban: 'Brief description of the department',
			motathem: 'Mô tả thêm về tình trạng dự án/phòng ban hiện tại',
			muctieu: 'Milestone',
			nguoiquantri: 'Admin',//Người quản trị
			nhanbanall: 'Duplicate all works and workgroups',
			nhanbancongviec: 'Duplicate work',
			nhanbanduan: 'Duplicate project',
			nhanbannhom: 'Duplicate work groups',
			no: 'No',
			note_closed: 'If you close this project/team, no new task could be added',
			note_reminders: 'Note: If stopped, reminders will be stopped forever',
			phanquyensudung: 'Phân quyền sử dụng',
			phongban: 'Department name',
			project: 'Project',
			team: 'Team',//phòng ban
			quahanganday: 'Recent overdue',
			quantrong: 'Important',
			quanlythanhvien: 'Manage member',//Quản lý thành viên
			reminders: 'Stop task reminders',
			tabchinhsua: 'Edit',//Chỉnh sửa
			tabcongviec: 'Work',// Công việc
			tags: 'Tags',
			taocongviec: 'Create work',//Tạo công việc
			taonhomcongviec: 'Create work groups',//Tạo nhóm công việc
			tenduan: 'Project name',//Tên dự án
			tenmuctieu: 'Milestone name',//Tên mục tiêu
			thanhvien: 'Member',//Thành viên
			thanhvienduan: 'Project members',
			thanhvienkhac: 'Orther members',
			thanhvienquantri: 'Admin members',//Thành viên quản trị
			thaydoithanhcong: 'Project/Team type changed successfully',//Thay đổi loại project/Team thành công
			themcongviec: 'Create work',//Thêm công việc
			themmoidepartment: 'Create department',//Thêm ban mới
			themmoiproject: 'Create project',//Thêm dự án mới
			themmoiteam: 'Create group',//Thêm nhóm mới
			thongke: 'Statistical',
			thongketheothanhvien: 'Statistics by member',
			thongtinchung: 'General overview',//GENERAL OVERVIEW
			timnhanhcongviec: 'Search work',//Tìm nhanh công việc
			tuychinh: 'Setting',//Tùy chỉnh
			trangthaiduan: 'Project status',//Trạng thái dự án
			update_message: 'Update message',
			xemchitietduan: 'View projectteam details',
			yes: 'Yes',
			mieutachung: 'General description',
			tuychon: 'Orther options',
			allow_percent_done: 'Allow to complete the work percent',//Cho phép hoàn thành phần trăm công việc
			not_allow_percent_done: 'Percentage completion is not allowed',//Không Cho phép hoàn thành phần trăm công việc
			allow_estimate_time: 'Cho phép thời lượng công việc dự kiến',
			not_allow_estimate_time: 'Không cho phép thời lượng công việc dự kiến',
			trangthaihientai: 'Current state',
			require_evaluate: 'Yêu cầu đánh giá trước khi đánh dấu hoàn thành công việc',
			evaluate_by_assignner: 'Tự động thiết lập người giao việc là người đánh giá',
			not_evaluate_by_assignner: 'Không tự động thiết lập người giao việc là người đánh giá',
			khongyeucau: 'Không yêu cầu',
			yeucaudanhgia: 'Yêu cầu đánh giá',
			dangthuchien: 'Processing',// đang thực hiện
			dong: 'Close',
			thoigianthuchien: 'Execution time',//Thời gian thực hiện
			tuychonhienthi: 'Options to show department work',//Tùy chọn hiển thị công việc của phòng ban
			tonghoptheochuky: 'Tổng hợp theo chu kỳ',
			hangthang: 'Monthly',
			hangtuan: 'Weekly',
			mausachienthi: 'Dislay color',//Màu sắc hiển thị
			default_view: 'Default view',
			thaotaccongviec: 'Các thao tác liên quan đến công việc',
			email_assign_work: 'Email assign work',
			email_update_work: 'Email update work',
			email_update_status: 'Email cho người được giao việc khi trạng thái công việc được cập nhật (doing-done)',
			email_delete_work: 'Email when deleted work',
			email_update_team: 'Email when the team is edited',
			email_delete_team: 'Email when deleted team',
			email_upload_file: 'Email when new document are available',
			group1: 'Phân quyền sử dụng theo loại tài khoản',
			group2: 'Phân quyền chỉnh sửa cập nhật công việc',
			group3: 'Giới hạn đặc biệt đối với người được giao việc',
			phanquyen: 'Phân quyền',
			admin: 'Admin',
			member: 'Member',
			customer: 'Customer',
			trichxuatcongviec: 'Trích xuất công việc',
			trichxuatcongvieccon: 'Trích xuất công việc con',
			cancutheo: 'Base on',
			ngaytao: 'Created date',
			deadline: 'Deadline',
			danhsachnhan: 'List tags',
			themnhanmoi: 'Create tag',
			admins: 'Admins',//Thành viên quản trị
			members: 'Members',//Thành viên thực hiện
			moreoptions: 'Add advanced options',//Thêm lựa chọn nâng cao
			lessoptions: 'Ẩn bớt các tùy chọn',
			color: 'Colors',//Màu đặc trưng
			chonngay: 'Selected date',//Chọn ngày
			filter: 'Filter',//Bộ lọc
			filter_tuychonhienthi: 'Display options',//Tùy chọn hiển thị
			report: 'Report',

		},
		notify: {
			thongbaochuyentrang: 'Page transfer notice',
			bancomuonchuyendentrang: 'De you want to go to the page ',//Bạn có muốn chuyển đến trang
			dangchuyentrang: 'Redirecting pages',//Đang chuyển trang
			chuanhapduthongtinbatbuoc: 'Chưa nhập đủ trường thông tin bắt buộc!',
			filedatontai: 'File already exists',//File đã tồn tại
			vuilongchonthanhvien: 'Please select a member',//Vui lòng chọn thành viên
			vuilongchonfiledulieu: 'Please select the data file',//Vui lòng chọn file dữ liệu
			importthanhcong: 'Imported successfully',//thành công
			trongso: 'trong số',
			chuacothanhviennao: 'No member',//Chưa có thành viên nào
			sodongtrentrang:'Items per page',//Số dòng trên trang
			trongtongso:'of',
			
		},
		filter:
		{
			tuan: 'WEEK',//
			active: 'Active',
			add: 'Add filter',
			ansubtask: 'Hide subtasks',
			chamtiendo: 'Off track',
			choreview: 'Reviewing', 
			choduyet: 'Pending',
			congvieccon: 'Child work',
			congviecduocgiao: 'Assigned tasks',
			congviecgiaodi: 'Created tasks',
			customfilter: 'Custom filter',
			dadong: 'Closed',
			dangdanhgia: 'Reviewing',
			danglam: 'Doing',
			dangthuchien: 'Processing',
			daxong: 'Done',
			daduyet: 'Approved',// đã duyệt
			deadline: 'Deadline',
			dungtiendo: 'On track',//Đúng tiến độ
			edit: 'Edit custom filter',
			filterhoatdong: 'Activity search',
			filtertailieu: 'Decuments search',
			gansao: 'Start mark',//Đánh dấu sao
			giaoboitoi: 'Delivered by me',//Giao bởi tôi
			giaochotoi: 'Assign for me',// giao cho tooi
			giaovaduocgiao: 'Delivered and assigned',
			hienthisubtask: 'Show subtasks',
			hoanthanh: 'Completed',//Hoàn thành
			htmuon: 'Complete late',//Hoàn thành muộn
			htsau: 'Late completed',//HT sau deadline
			htquahan: 'Done late',//HT quá hạn
			huy: 'Cancel',
			khancap: 'Urgency',//Khẩn cấp
			khonghienthi: 'Not displayed',//Không hiển thị
			kichhoat: 'Actived',
			luachonthanhvien: 'Select member',
			moicapnhat: 'Update',//Mới cập nhật
			motcapcongvieccon: 'Một cấp công việc con',
			muctieu: 'Milestone',//Mục tiêu
			nhieucapcongvieccon: 'Nhiều cấp công việc con',
			nhomboi: 'Group by',
			ngaytao: 'Created date',
			nhomcongviec: 'Work groups',
			nhomcongviechoanthanh: 'Compelete work group',
			nomilestone: 'No milestone',
			operators: 'Operators',
			options: 'Options',
			phailam: 'Todo',
			prioritize: 'Prioritize',
			quahan: 'Overdue',//Quá hạn
			quantrong: 'Important',
			remove: 'Remove filter',
			ruirocao: 'At risk',//Rủi ro cao
			sapxep: 'Sort',//Sắp xếp
			save: 'Save filter',
			select: 'Select another filter',
			tatca: 'All',
			tatcaduan: 'All projects',//Tất cả dự án
			tatcamuctieu: 'All milestone',//Tất cả mục tiêu
			tatcatrangthai: 'All states',
			tenfilter: 'Filter name',
			thanhvien: 'Member',
			theobanchucai: 'According to the alphabet',
			thoigiancapnhat: 'Time to update',//Thời gian cập nhật
			thoigianhoanthanh: 'Completion time',//Thời gian hoàn thành
			thoigiantao: 'Create time',
			thoihangiamdan: 'Term (descending)',//Thời hạn (giảm dần)
			thoihantangdan: 'Term (ascending)',//Thời hạn (tăng dần)
			thutuuutien: 'Priority',//Thứ tự ưu tiên
			trangthai: 'status',
			tuychonhienthi: 'Display option',
			vieckhancap: 'Việc khẩn cấp',
			vohieuhoa: 'Disable',
			ngaybatdau: 'Start date',
			ngayketthuc: 'End date',
			locked: 'Locked',
			_active: 'Active',
			htdunghan: 'Done ontime',
			dangcho: 'Waiting...',//Đang chờ
			congviectrong: 'Work in',//Công việc trong
			datao: 'Created',
			capnhatcuoi: 'Last update',//Cập nhật cuối
			tasklist: 'Tasklist',
			setmilestone: 'Set milestone',
			tag: 'Tag',
			settag: 'Set tag',
			soluongcongviec: 'Number of tasks',//Số lượng công việc
			tilehoanthanh: 'Completion rate',//Tỉ lệ hoàn thành
			detail: 'Detail',
			tu: 'From',
			thanh: 'to',//thành
			luc: 'at',//lúc
			hoac: 'or',
			logdetail: 'LOG DETAIL',
			chonduan: 'Select project',
			tatcaphongban: 'All departments',
			tatcacongviec: 'All works',
			congviecvacongvieccon: 'Work and child work',
			khongtinhcongvieccon: 'No has child work',// không tính công việc con
			congviecdangthuchien: 'Work in process',//Công việc đang thực hiện
			congviecdahoanthanh: 'The work has been completed',//Công việc đã hoàn thành
			congviecquahan: 'The work late deadline',//Công việc đã hoàn thành
			congviec: 'Work',//Công việc
			nhomdadong: 'Group closed',
			hoanthanhmuon: 'Complete late',
			tren: 'on',
			chuacocongviec: 'No work yet',
			thoihan: 'Duration',//Thời hạn
			phutrach: 'In charge',
			trangdau:'First page',
			trangcuoi:'Last page', // last page
			trangke:'Next page', // next page
			trangtruoc:'Previous page',//previous page
		},
		topic: {
			timkiemthaoluan: 'Search discusion',//Tìm kiếm thảo luận
			themmoithaoluan: 'Create discusion',//Thêm mới thảo luận
			chinhsua: 'Edit topic',
			xoa: 'Delete topic',
			upload: 'Upload more file',
			addfollowers: 'Add follower',//Add followers
			themnguoitheodoitungoai: 'Add followers from outsite',
			themtungoai: 'Add from outsite',
			xoafile: 'Delete file upload',
			tieudethaoluan: 'Discussion title',
			nguoitheodoi: 'Follower',
			noidungthaoluan: 'Discussion content',
			projectteam: 'Project team',
			follow: 'Follow',
			un_follow: 'Unfollow',
			favourite: 'favourite',
			un_favourite: 'favourite',
			assign: 'Assign',
			guimail: 'Email discussion',//Gửi nội dung thảo luận qua Email
			tuychinh: 'Setting',//Tùy chỉnh
			thaoluan: 'Discussion',//Thảo luận
			closetopic: 'Close this topic',
			xemthongtin: 'View infomation',
			nhantinvoi: 'Chat with',
			xoatheodoi: 'Delete follower',
			suabinhluan: 'Edit comment content',//Sửa nội dung bình luận
			xoabinhluan: 'Delete comment',//Xóa bình luận
			binhluan: 'Comment',//Bình luận
			like: 'Like',
			reply: 'Reply',
			chatvoi: 'Chat with',
			xemprofilecua: 'View profile of',
			boquyen: 'Bỏ quyền',
			thaoluancunhat: 'Oldest topic',
		},
		setting: {
			list: 'List view',
			gantt: 'Table view with Gantt',
			stream: 'Stream view',
			period: 'Period view ',
			weeklyormonthly: '(weekly or monthly)',
			board: ' Board (kanban) view',
			default: '(default)',
		},

		color: {
			red: 'red',
			green: 'Green'
		},
		summary: {
			moiduocgiao: 'Recently assigned',
			moigiaodi: 'Recently assigning',
			luuy: 'Note',
			muctieu: 'Milestone',
			boloctuychinh: 'Custom filters',
			nhanviencuatoi: 'My staff',
			nguoigiaoviec: 'ASSIGNER',
			nguoitheodoi: 'Follower',
			themtungoai: 'Add from outsite',
			lichsuhoatdong: 'Operation history',
			chuacomuctieu: 'No linked milestone',//Chưa liên kết với mục tiêu
			thoigiantao: 'Creation time',// TG tạo
			thoigiancapnhat: 'Time to update',//TG cập nhật
			batdau: 'Start',// bắt đầu
			deadline: 'Deadline',
		},
		work:
		{
			nhanviencuatoi: 'My staff',//Nhân viên của tôi
			addsubtask: 'Add sub task',
			binhthuong: 'Normal',//Bình thường
			boloc: 'Filter',
			capnhatketquacongviec: 'Update work results',//Cập nhật kết quả công việc
			chinhsuanhomcongviec: 'Workgroup editing',//Chỉnh sửa nhóm công việc
			chinhsuathongtincongviec: 'Edit work infomation',//Chỉnh sửa thông tin công việc
			co: 'Có, cần gửi lại kết quả công việc',
			congviec: 'Work',
			congviecchuahoanthanhtuthoigiantruocdo: 'The work is not compelted from before time',
			congvieccon: 'Child work',
			cothoihanhaykhong: 'Có thời hạn hay không?',
			createsubtask: 'Create sub task',
			cvlaplai: 'Repeat task',
			dangtheodoi: 'Following',
			danhsachcacngay: 'List of date, separated by commas',//Danh sách ngày, cách nhau bằng dấu phẩy
			chonduantruockhigiaoviec: 'Select project before assign',//Chọn dự án trước khi giao việc
			danhsachcankiemtra: 'List to ckech',
			danhsachnhan: 'List tags',
			datthoihanchocongviec: 'Set deadline for work',
			deadline: 'Deadline',
			dscongviec: 'List works',
			giaolaichonguoikhac: 'Handing them over to order',//Giao lại cho người khác
			giaoviec: 'Tag @ to assign',
			ketquacongviec: 'Work results',//KẾT QUẢ CÔNG VIỆC
			khancap: 'Urgency',
			khongdatthoihan: 'Dont set deadline',
			khongyeucau: 'Not requied',
			lichbieu: 'Calendar',
			loai: 'Type',//loại
			luc: 'at',//lúc
			mieutacongviec: 'Description of work',//MIÊU TẢ CÔNG VIỆC
			motacongviec: 'Description of work',//Mô tả công việc
			motanhomcongviec: 'Description of workgroup',//Mô tả cho nhóm công việc
			motathemvecongviec: 'Mô tả thêm về công việc',
			mucdouutien: 'Priority level?',
			ngaybatdaucongviec: 'Date of starting work',
			nguoigiaoviec: 'Personal assiging work',//Người giao việc
			nhanbancongviec: 'Duplicate work',//Nhân bản công việc
			nhanbancongviecvaoprojectkhac: 'Duplicate work into another project',//Nhân bản công việc vào dự án khác
			nhantinvoi: 'Chat with',
			noduplicate: 'No, duplicate as ordinary task',
			parent: 'Duplicate as subtask of',
			suanguoireiewer: 'Edit reviewer',//Sửa người reviewer
			tagtoassign: 'Tag @ to assign',
			tailieudinhkem: 'Attachments',//TÀI LIỆU ĐÍNH KÈM
			taobansaococongvieckemtheo: 'Make a copy with attached work',//Tạo bản sao có công việc kèm theo
			tencongviec: 'Task name',//Tên công việc
			tennhomcongviec: 'Tasklist',
			thanhvientheodoi: 'Members follow up',
			thanhvienthuchien: 'Thành viên thực hiện',
			themboloctuychinh: 'Add custom filter',
			suaboloctuychinhhientai: 'Update this custom filter',
			themchecklist: 'Add check list',
			themmoicongviec: 'Create work',//Thêm mới công việc
			themnhanhnhanmoi: 'Create tag',//Thêm nhanh nhãn mới
			themnhieunguoitheodoi: 'Add more follower',//Thêm nhiều người theo dõi
			themtailieu: 'Add file',//Thêm tài liệu
			thuocnhomcongviec: 'Thuộc nhóm công việc',
			time: 'Time (hh:mm, optional)',
			tongquan: 'Overview',
			upload: 'Upload attachment',
			uploadfile: 'Upload file',
			xemthongtin: 'View profile',//Xem thông tin
			xoacongviec: 'Delete work',
			xoanhomcongviec: 'Delete workgroup',
			yeucauketquacongviec: 'Request work results',
			bansaocua: 'Copies of',
			vaoduankhac: 'into another project',//vào dự án khác
			taomoicongviec: 'Create work',//Tạo mới công việc
			chinhsuacongviec: 'Update work',//Chỉnh sửa công việc
			selectedtag: 'Selected Tag',
			selectedmilestone: 'Selected Milestone',
			dachon: 'Selected',//Đã chọn
			nhapnoidungdegui: 'Enter the test and press the Enter key to send',//Nhập nội dung và nhấn phím Enter để gửi
			postnow: 'Post now',
			cancel: 'Cancel',
			click_seclect_emotion: 'CLICK TO SELECT AN EMOTION YOU LIKE',
			managedby: 'Managed by',
			people: 'people',
			accessibleby0usergroups: 'Accessible by 0 user groups',
			xemchitiet: 'View detail',//Xem chi tiết
			chinhsuanhanh: 'Quick editing',//Chỉnh sửa nhanh
			capnhattiendo: 'Progress update',//Cập nhật tiến độ
			quanly: 'Manage',
			tagthemthanhvien: 'Use @ to additional members',//Sử dụng @ để tag thêm thành viên
			submit: 'Submit',
			nothanks: 'No Thanks',
			nhapcongviectuexcel: 'Improt work from Excel file',
			capnhatcongviectuexcel: 'Quickly update work from Excel file',//Cập nhật nhanh công việc từ file Excel
			xemfilemau: 'View sample file',
			hotro: 'Support',
			themnhieu: 'Add more',
			ok: 'OK',
			congviecquahan:'Task overdue',
			linkto:'Linked project/team',
			thongkemuctieu:'Statistics based on deadlines',
			personalincharge:'PERSON IN CHARGE',
			motamuctieu:'Milestone description',
			listoftask:'List of task',

		},
		wuser: {
			list: 'Members list',
			timkiemnhanh: 'Search for members',
			hoten: 'Fullname',
			contact: 'Contact',
			mananger: 'Manager',
			congviecduocgiao: 'Assigned work',
			in: 'Print',
			export: 'Export excel',
			uyquyen: 'Authorized work',
			theoduan: 'Divided by project',
			congviec: 'Tasks',
			tungay: 'From',
			denngay: 'To',
			capnhat: 'Update',
			duan_phongban: 'Project/Team',
			timkiemcv: 'Search work',
			phailam: 'Todo',
			danglam: 'Doing',
			hoanthanh: 'Completed',
			quahan: 'Overdue',
			htquahan: 'Completed late',
			khancap: 'Urgency',
			quantrong: 'Important',
			sapxeptheo: 'Sort by',
			chonnguoiuyquyen: 'Chọn người ủy quyền',
			uyquyengiaoviec: 'ỦY QUYỀN GIAO VIỆC',
			chonnguoireviewer: 'Select reviewer',
			tonghopsapxeptheo: 'Sorted by',// tổng hợp sắp xếp theo
			tonghoptheo: 'Collected by',
		},
		mystaff: {
			duanteam: 'PROJECT & TEAM',
			congviec: 'Tasks',
			hoanthanh: 'Completed',
			danglam: 'Doing',
			dangdanhgia: 'REVIEWING',
			htsau: 'Late completed',
			quahan: 'OVERDUE',
			tile: 'Total`'//TỈ LỆ
		},
		tags:
		{
			tag: 'Tag',
			themtag: 'Add tag',
			htquahan: 'Late completed',
			vao: 'into the',//vào
		},
		day: {
			t2: 'Mon',
			t3: 'Tue',
			t4: 'Wed',
			t5: 'Thu',
			t6: 'Fri',
			t7: 'Sat',
			cn: 'Cun',
			thu2: 'Monday',
			thu3: 'Tuesday',
			thu4: 'Wednesday',
			thu5: 'Thusday',
			thu6: 'Friday',
			thu7: 'Saturday',
			chunhat: 'Sunday',
			thuhai: 'Monday',
			thuba: 'Tuesday',
			thutu: 'Wednesday',
			thunam: 'Thusday',
			thusau: 'Friday',
			thubay:  'Saturday',
			tuannay: 'This week',
			thangnay: 'This month',
			homnay: 'To day',
			xemtuantruoc: '<< View last week',
			xemthangtruoc: '<< View last month',
			xemtuansau: 'View next week >>',
			xemthangsau: 'View next month >>',
			thu: "Thứ",
			mathangkhac: "other items",
			dinhdang: 'Format',
			chonngay: 'SELECT DURATION',//SELECT DURATION
			ngaybatdau: 'Start day',
			theongaytao: 'By created date',
			theothoihan: 'By deadline',
			theongaybatdau: 'By start date',
			theongaycapnhat: 'By updated date',
		},
		report: {
			baocaocacphongban: 'Report to departments',//Báo cáo các phòng ban
			baocaochitiettheothanhvien: 'Task assignments by members',
			baocaocongviec: 'REPORT ABOUT TASKS',
			baocaotonghopduan: 'General report by project',//Báo cáo tổng hợp theo dự án
			baocaotrangthaicongviec: 'Task breakdown by statuses',//Báo cáo trạng thái công việc
			congviec: 'Tasks',
			congviecdahoanthanh: 'The task has been completed',//Công việc đã hoàn thành
			congviecduoctaokhongcothoihan: 'tasks are created without deadline',
			congviecdangdanhgia: 'Tasks in review',
			congvieckhongdunghan: 'Late task reports',
			connhieuviecnhat: 'Most works ahead',
			dangthuchien: 'Processing',// đang thực hiện
			department: 'Department',
			duan: 'Project',
			duanhuy: 'Canceled project',//Dự án hủy
			duankhachhang: 'Dự án làm việc riêng với khách hàng',
			duannoibo: 'internal project',
			duanbenngoai: 'outsite project',
			duanphongban: 'project team',
			duanthanhcong: 'successful project',//dự án thành công
			duanthatbai: 'project failed',//dự án thất bại
			hoanthanh: 'completed',
			khach: 'customer account ',
			lammuonnhieunhat: 'Top delayed',//Làm muộn nhiều nhất
			matraneisenhower: 'Eisenhower matrix',//Ma trận Eisenhower
			muctieu: 'Milestone',
			nhanvien: 'employee account',
			phongban: 'Department',
			phongbanbenngoai: 'outside departments',
			phongbannoibo: 'internal departments',
			quatrinhhoanthanhtheongay: 'Daily progressions',
			soluongcongviec: 'Number of tasks',
			thanhvien: 'Member',
			thanhvienxuatsac: 'Top performers',
			tagclounds: 'Tag clounds',
			tonghoptheotuan: 'Weekly reports',//Tổng hợp theo tuần
			xuatbaocaoexcel: 'Export Excel',
			danhgia: 'In review',
			dangdanhgia: 'Reviewing',
			phongbanvaduan_reviewcv: 'teams and projects enabling task reviews',
			congviecdangthuchien: ' work in progress',//công việc đang thực hiện
			chitinhcaccongviecbaocaotrongthoigianluachon: 'Only reported tasks during the selected duration are measured',
			cacbaocaochuyensaukhac: 'OTHER INSIGHTS',
			muctieutheodepartment: 'Milestone by department',
			tophoanhthanhmuctieu: 'Top finishers',
			cacconsothongke: 'Unit statistics',
			phanbocongviectheodepartment: 'Distribution of work by department',
			baocaotheodepartment: 'Reported by department',
			baocaotonghoptheoduan: 'Reported by project',//Báo cáo tổng hợp theo dự án
			congviecnhieunhat: 'Most works ahead',
			socongviectrungbinh: 'Avg tasks created',
			socongviectrungbinh1: 'Avg number of tasks',
			socongviectrungbinhhangtuan: 'Avg tasks per weekly',
			trenmottuan: 'per week',
			trenmotduan: 'per project',
			trenmotnguoi: 'per person',
			binhluan: 'comments',
			tongsoluong: 'Total number of',
			danhsachmuctieu: 'List of milestones',
			baocaotheodanhsachmuctieu: 'REPORT ABOUT MILESTONES',
			phongbanhoatdong: 'Active teams',//Phòng ban hoạt động
			clicktoupdate: 'Click to updates',

		},
		repeated:
		{
			tansuatlaplai: 'Task frequency *',
			cacngaylaplai: 'Repeated days * (List of all repeated days)',//Repeated days * List of all repeated days
			hangtuan: 'Weekly',
			hangthang: 'Monthly',
			forcerun: 'Force run',
			nhanban: 'Repeated',
			chinhsuasubtask: 'Edit subtask',
			chinhsuatodolist: 'Edit todoList',
			chinhsubtask: 'Edit subtask',
			chinhsuatodo: 'Edit to do',
			deadline: 'Deadline (Hours added). ex: 8, 8.5, Leave it blank if there is no deadline',
			todo: '#To do',
			subtask: '#Sub task',
			subtasks: 'SUB TASKS',
			checklist: 'CHECK LISTS',
			assign: 'Assign to',
			deadline_hour: 'Deadline (Hours)',
			forcerun_confirm: 'Force initializing this repeated task?',
			forcerun_doing: 'Data is running...',
			forcerun_thanhcong: 'Run successfully (1 task created)',
			forcerun_thongbaoloi: 'CANNOT RUN THE SCHEDULED TASK - status.disable. You can only force run a repeated task once (please check the start date and end date too)',
			tuychonnangcao: 'MORE ADVANCED OPTIONS', // MORE ADVANCED OPTIONS
			subtask1:'subtasks',
			todos:'todos',
		},

		//=========================================LandingPage================================================
		phepcanhan: {
			tieude1: 'NGHỈ PHÉP/CÔNG TÁC',
			thoigian: 'Time',
			tinhtrang: 'Tình trạng',
			ngaygui: 'Ngày gửi',
			nguoiduyet: 'Người duyệt',
			ngayduyet: 'Ngày duyệt',
			lydo: 'Lý do',
			chontinhtrang: 'Chọn tình trạng',
			choduyet: 'Chờ duyệt',
			chuaduyet: 'Chưa duyệt',
			daduyet: 'Đã duyệt',
			khongduyet: 'Không duyệt',
			khongtimthaynguoi: 'Không tìm thấy người',
			guicho: 'Gửi cho',
			duyet: 'duyệt',
			ngaybdnghi: 'Ngày bắt đầu nghỉ',
			denngay: 'Đến ngày',
			gionghi: 'Từ giờ',
			dengio: 'Đến giờ',
			hinhthuc: 'Hình thức',
			songay: 'Số ngày',
			themmoi: 'ĐĂNG KÝ NGHỈ PHÉP/CÔNG TÁC',
			chinhsua: 'CẬP NHẬT NGHỈ PHÉP/CÔNG TÁC',
			tieude2: 'Số phép còn lại',
			nam: 'Năm',
			tongngayphep: 'Tổng ngày phép năm được hưởng',
			songayphepthamnien: 'Số ngày phép thâm niên',
			pheptonnamtruoc: 'Phép tồn năm trước',
			ngayhethanpheptonnamtruoc: 'Ngày hết hạn phép tồn năm trước',
			phepduochuong: 'Phép được hưởng trong năm tính đến thời điểm hiện tại',
			nghitruphepton: 'Nghỉ trừ phép tồn năm trước',
			nghitruphepnam: 'Nghỉ trừ phép năm hiện tại',
			tongsongay: 'Tổng số ngày nghỉ còn lại',
			ngayvaolam: 'Ngày vào làm',
			sophepconlai: 'Số phép còn lại',
			timkiemsearch: 'Tìm kiếm theo tên loại phép',
			hanmuccacloaiphep: 'HẠN MỨC PHÉP',
			loaiphep: 'Loại phép',
			tonnamtruoc: 'Tồn năm trước',
			sudungden: 'Sử dụng đến',
			tongduoccap: 'Tổng được cấp đến hiện tại',
			tongdanghi: 'Tổng đã nghỉ',
			conlai: 'Còn lại',
			pheptrongnam: 'Phép trong năm',
			thamnien: 'Thâm niên',
			nghiphepton: 'Nghỉ phép tồn',
			nghipheptrongnam: 'Nghỉ phép trong năm',
			diadiem: 'Địa điểm',
			nghididulich: 'Đi du lịch',
		},
		duyetnghiphep: {
			tieude1: 'Duyệt nghỉ phép/công tác',
			chontinhtrang: 'Chọn tình trạng',
			nhanvien: 'Nhân viên',
			thoigian: 'Time',
			loaiphep: 'Hình thức',
			duyettatca: 'Duyệt tất cả',
			khongduyettatca: 'Không duyệt tất cả',
			duyet: 'Duyệt',
			tralai: 'Trả lại',
			khongduyet: 'Không duyệt',
			choduyet: 'Chờ duyệt',
			daduyet: 'Đã duyệt',
			thanhcong: 'Thành công',
			tieude2: 'ĐƠN XIN NGHỈ PHÉP',
			kinhgui: 'Kính gửi',
			bangiamdoc: 'Ban giám đốc công ty',
			toitenla: 'Tôi tên là',
			maso: 'Mã số',
			chucvu: 'Chức vụ',
			bophan: 'Bộ phận',
			tieude3: 'Nay tôi làm đơn này xin Ban Giám Đốc cho tôi được nghỉ phép',
			ngay: 'ngày',
			batdautu: 'Bắt đầu từ',
			den: 'đến',
			hinhthuc: 'Hình thức',
			lydo: 'Lý do',
			tieude4: 'Kính gửi Ban Giám Đốc xem xét giải quyết cho tôi, tôi chân thành cảm ơn.',
			hochiminh: 'TP. Hồ Chí Minh',
			ngayduyet: 'Ngày duyệt',
			nguoiduyet: 'Người duyệt',
			ghichu: 'Ghi chú',
		},
		uyquyen: {
			tieude: 'Ủy quyền duyệt phiếu',
			tieude1: 'Người ủy quyền',
			tieude2: 'Người được ủy quyền',
			tieude3: 'Nội dung ủy quyền',
			phongban: 'Phòng ban',
			nhanvienuyquyen: 'Nhân viên ủy quyền',
			nhanvienduocuyquyen: 'Nhân viên được ủy quyền',
			thoigiantu: 'Thời gian từ',
			den: 'đến',
			loaiphieuduyet: 'Loại phiếu duyệt',
			tennguoiuyquyen: 'Tên người ủy quyền',
			nguoiduocuyquyen: 'Người được ủy quyền',
			chucvu: 'Chức vụ',
			tungay: 'Từ ngày',
			denngay: 'Đến ngày',
			themmoi: 'Thêm mới ủy quyền duyệt phiếu',
			chinhsua: 'Chỉnh sửa ủy quyền duyệt phiếu',
		},
		pheptonnhanvien: {
			tieude1: 'Phép tồn của nhân viên',
			manv: 'Mã NV',
			hoten: 'Họ tên',
			chucvu: 'Chức vụ',
			phepnamconlai: 'Phép năm còn lại',
			nghibuconlai: 'Nghỉ bù còn lại',
			loaiphep: 'Loại phép',
			phepton: 'Phép tồn',
			ngayhethan: 'Ngày hết hạn',
			phepduochuong: 'Phép được hưởng',
			nghitruphepton: 'Nghỉ trừ phép tồn',
			nghitruphepnam: 'Nghỉ trừ phép năm',
			tong: 'Tổng',
		},
		dangkyphep: {
			tieude: 'Đăng ký phép cho nhân viên',
			nghiphepnam: 'Nghỉ phép năm',
			nghibu: 'Nghỉ bù',
			ngaybdnghi: 'Ngày bắt đầu nghỉ',
			hinhthuc: 'Hình thức',
			songay: 'Số ngày',
			ghichu: 'Ghi chú',
			denngay: 'Đến ngày',
			ngaygui: 'Ngày gửi',
			ngayduyet: 'Ngày duyệt',
			nghitu: 'Nghỉ từ',
			den: 'Đến',
			vaolamthucte: 'Vào làm thực tế',
			themmoi: 'Thêm mới phép',
			chinhsua: 'Chỉnh sửa phép',
			gionghi: 'Từ giờ',
			dengio: 'Đến giờ',
			songaysudung: 'Số ngày được sử dụng:',
			sang: 'Sáng',
			chieu: 'Chiều',
			daubuoi: 'Đầu buổi',
			cuoibuoi: 'Cuối buổi',
			denngayvaolam: 'Ngày vào làm',
			buoi1: 'Buổi ngày nghỉ',
			buoi2: 'Buổi vào làm',
		},
		danhsachnghiphep: {
			tieude1: 'Danh sách nghỉ phép/công tác',
			manv: 'Mã NV',
			hoten: 'Họ tên',
			tungay: 'Từ ngày',
			denngay: 'Đến ngày',
			songay: 'Số ngày',
			hinhthuc: 'Hình thức',
			lydo: 'Lý do',
			thoigian: 'Time',
			excel: 'Xuất excel',
		},
		quanlyduyet: {
			tieude: 'QUẢN LÝ DUYỆT',
			timkiemsearch: 'Tìm kiếm theo từ khóa',
			loaiyeucau: 'Loại yêu cầu',
			duyetgiaitrinh: 'Duyệt giải trình chấm công',
			duyetnghiphep: 'Duyệt nghỉ phép/công tác',
			duyetdoica: 'Duyệt đổi ca làm việc',
			duyettangca: 'Duyệt tăng ca',
			duyetthoiviec: 'Duyệt thôi việc',
			duyettuyendung: 'Duyệt yêu cầu tuyển dụng',
		},
		lylichnhanvien: {
			trolai: 'Trở lại',
			xuatfile: 'Xuất file lý lịch',
			manhanvien: 'Mã số nhân viên',
			hoten: 'Họ tên',
			donvi: 'Đơn vị',
			phongban: 'Phòng ban',
			chucvu: 'Chức vụ',
			namsinh: 'Năm sinh',
			xem: 'Xem',
			thongtinnhanvien: 'THÔNG TIN NHÂN VIÊN',
			thongtincanhan: 'Thông tin cá nhân',
			ngaysinh: 'Ngày sinh',
			gioitinh: "Giới tính",
			noisinh: 'Nơi sinh',
			nguyenquan: 'Nguyên quán',
			diachithuongtru: 'Địa chỉ thường trú',
			diachitamtru: 'Địa chỉ tạm trú',
			dienthoainha: 'Điện thoại nhà',
			dienthoai: 'Điện thoại',
			emailcongty: 'Email công ty',
			sosobhxh: 'Số sổ BHXH',
			tinhtranghonnhan: 'Tình trạng hôn nhân',
			masothue: 'Mã số thuế',
			cmnd: 'CMND',
			ngaycapcmnd: 'Ngày cấp CMND',
			noicapcmnd: 'Nơi cấp CMND',
			trinhdohocvan: "Trình độ học vấn",
			truongtotnghiep: 'Trường tốt nghiệp',
			chuyenmon: 'Chuyên môn',
			trinhdotinhoc: 'Trình độ tin học',
			dantoc: 'Dân tộc',
			tongiao: 'Tôn giáo',
			trinhdongoaingu: 'Trình độ ngoại ngữ',
			bangcapkhac: 'Thông tin bằng cấp khác',
			tenbangcap: 'Tên bằng',
			chungchi: 'Chứng chỉ',
			tentruong: 'Trường tốt nghiệp',
			tennganh: 'Ngành tốt nghiệp',
			namtotnghiep: 'Năm tốt nghiệp',
			nguoithan: 'Người thân',
			nguoithan1: '(Cha, mẹ, chồng (hoặc vợ), con cái, anh em)',
			quanhe: 'Quan hệ',
			noio: 'Nơi ở',
			ngoaingu: 'Ngoại ngữ',
			bang: 'Bằng',
			ghichu: 'Ghi chú',
			thongtincongviec: 'Thông tin công việc',
			hopdong: 'Danh sách hợp đồng đã ký',
			sohopdong: 'Số hợp đồng',
			ngayky: 'Ngày ký',
			ngaycohieuluc: 'Ngày có hiệu lực',
			ngayhethan: 'Ngày hết hạn',
			nguoiky: 'Người ký',
			quatrinhdaotao: 'Quá trình đào tạo',
			quatrinhcongtac: 'Quá trình công tác',
			truockhivaocongty: 'Trước khi vào công ty',
			tu: 'Từ',
			den: 'Đến',
			congviec: 'Công việc',
			noilamviec: 'Nơi làm việc',
			quatrinhcongtactaicongty: 'Quá trình công tác tại công ty',
			tungay: 'Từ ngày',
			donvicongtac: 'Đơn vị công tác',
			chucdanh: 'Chức vụ',
			quatrinhdanhgia: 'Quá trình đánh giá',
			nam: 'Năm',
			tenchucdanh: 'Tên chức danh',
			tenloai: 'Tên loại',
			huongphattrien: 'Hướng phát triển',
			sangkien: 'Sáng kiến',
			quatrinhnghidaihan: 'Quá trình nghỉ dài hạn',
			ngayxinphep: 'Ngày xin phép',
			ngaynghi: 'Ngày nghỉ',
			tongthoigiannghi: 'Tổng thời gian nghỉ',
			lydo: 'Lý do',
			khenthuongkyluat: 'Khen thưởng/Kỷ luật',
			khenthuong: '1. Khen thưởng',
			ngaythuong: 'Ngày quyết định',
			hinhthuc: 'Hình thức',
			kyluat: '2. Kỷ luật',
			ngayquyendinh: 'Ngày quyết định',
			loivipham: 'Lỗi vi phạm',
			hinhthucxuly: 'Hình thức xử lý',
			ngaybatdau: 'Ngày bắt đầu làm việc',
			ngaychinhthuc: 'Ngày vào chính thức',
			quanly: 'Quản lý hiện tại',
			xuaths02: 'Xuất file mẫu HS02'
		},
		dangkyphep_cbcc: {
			dangkyphepchonhanvien: 'Đăng ký phép cho cán bộ',
			tungay: 'Nghỉ từ ngày',
			denngay: 'Đến ngày',
			buoi: 'Buổi',
			sang: 'Sáng',
			chieu: 'Chiều',
			hinhthuc: 'Hình thức',
			songay: 'Số ngày',
			ghichu: 'Ghi chú',
		},
		overtimeregister: {
			tieude: 'Đăng ký tăng ca',
			ngaytangca: 'Ngày tăng ca',
			tugio: 'Từ giờ',
			dengio: 'Đến giờ',
			sogio: 'Số giờ',
			cachtinh: 'Hình thức chi trả',
			phepbuduoccap: 'Phép bù được cấp',
			lydo: 'Lý do',
			duan: 'Dự án',
			ngaygui: 'Ngày gửi',
			ngayduyet: 'Ngày duyệt',
			Overtimeinbreaktime: 'Làm thêm trong giờ giải lao',
			congviec: 'Công việc',
			IsFixHours: 'Làm việc bên ngoài (Không chấm công)',
			timkiem: 'Tìm kiếm theo công việc',
		},
		duyettangca: {
			tieude1: 'Duyệt đăng ký công tác',
			chontinhtrang: 'Chọn tình trạng',
			HoTen: 'Họ tên nhân viên',
			ThoiGian: 'Time',
			duyettatca: 'Duyệt tất cả',
			khongduyettatca: 'Không duyệt tất cả',
			duyet: 'Duyệt',
			tralai: 'Trả lại',
			khongduyet: 'Không duyệt',
			choduyet: 'Chờ duyệt',
			daduyet: 'Đã duyệt',
			thanhcong: 'Thành công',
			GhiChu: 'Ghi chú',
			NgayTangCa: 'Ngày tăng ca',
			NgayGui: 'Ngày gửi tăng ca',
			congviec: 'Công việc',
			tieude2: 'PHIẾU ĐĂNG KÝ TĂNG CA',
			DuAn: 'Dự án',
			SoGio: 'Số giờ',
			batdautu: 'Bắt đầu từ',
			den: 'đến',
			hinhthuc: 'Hình thức',
			Lydo: 'Lý do',
			hochiminh: 'TP. Hồ Chí Minh',
			ngayduyet: 'Ngày duyệt',
			nguoiduyet: 'Người duyệt',
			hinhthucchitra: 'Hình thức chi trả',
			gio: 'Giờ'
		},
		duyetgiaitrinh: {
			tieude1: 'Duyệt giải trình chấm công',
			ngay: 'Ngày',
			giothucte: 'Giờ thực tế',
			loaigiaitrinh: 'Loại giải trình',
			giogiaitrinh: 'Giờ giải trình',
			lydo: 'Lý do',
			chitietgiaitrinh: 'Chi tiết giải trình chấm công',
		},
		duyetdoicalamviec: {
			tieude1: 'Duyệt đổi ca làm việc',
			chontinhtrang: 'Chọn tình trạng',
			nhanvien: 'Nhân viên',
			ngaygui: 'Ngày gửi',
			lydo: 'Lý do',
			duyettatca: 'Duyệt tất cả',
			khongduyettatca: 'Không duyệt tất cả',
			duyet: 'Duyệt',
			tralai: 'Trả lại',
			khongduyet: 'Không duyệt',
			choduyet: 'Chờ duyệt',
			daduyet: 'Đã duyệt',
			thanhcong: 'Thành công',
			tieude2: 'ĐƠN XIN ĐỔI CA LÀM VIỆC',
			kinhgui: 'Kính gửi',
			bangiamdoc: 'Ban giám đốc công ty',
			toitenla: 'Tôi tên là',
			maso: 'Mã số',
			chucvu: 'Chức vụ',
			bophan: 'Bộ phận',
			tieude3: 'Nay tôi làm đơn này xin Ban Giám Đốc cho tôi được nghỉ phép',
			chitiet: 'Chi tiết các ngày',
			tieude4: 'Kính gửi Ban Giám Đốc xem xét giải quyết cho tôi, tôi chân thành cảm ơn.',
			hochiminh: 'TP. Hồ Chí Minh',
			ngayduyet: 'Ngày duyệt',
			nguoiduyet: 'Người duyệt',
		},
		otapproval: {
			tieude1: 'Duyệt đăng ký tăng ca',
			tieude2: 'Danh sách tăng ca',
			ngaytangca: 'Ngày tăng ca',
			chonnhanvien: 'Chọn nhân viên',
			tugio: 'Từ giờ',
			dengio: 'Đến giờ',
			sogio: 'Số giờ',
			HinhThucChiTra: 'Hình thức chi trả',
			ThoiGian: 'Time',
			phepbuduoccap: 'Phép bù được cấp',
			Lydo: 'Lý do',
			DuAn: 'Dự án',
			NgayGui: 'Ngày gửi',
			ngayduyet: 'Ngày duyệt',
			Overtimeinbreaktime: 'Làm thêm trong giờ giải lao',
			TinhTienTangCa: 'Tính tiền tăng ca',
			TinhVaoPhepNghiBu: 'Tính vào phép nghỉ bù',
			SoGio: 'Số giờ',
			GioBatDau: 'Giờ bắt đầu',
			GioKetThuc: 'Giờ kết thúc',
			vuilongchonnhanviendethongke: 'Vui lòng chọn nhân viên để thống kê',
			vuilongchonhinhthucchitra: 'Vui lòng chọn hình thức chi trả',
			NgayTangCa: 'Ngày tăng ca',
			chondinhdanhxuat: 'Chọn định dạng xuất',
			khongchamcong: 'Làm việc bên ngoài',
			CongViec: 'Công việc',
		},
		dangkylichcongtac: {
			tieude: 'Đăng ký lịch công tác',
			theotuan: 'Theo tuần',
			theongay: 'Theo ngày',
			phongbanbophan: 'Departments',
			gui: 'Gửi đăng ký',
			bancochacchanmuongui: 'Bạn có chắc muốn gửi lịch đăng ký công tác',
			dulieudangduocxuly: 'Dữ liệu đang được xử lý',
			guithanhcong: 'Gửi thành công',
		},
		duyetdangkylichcongtac: {
			tinhtrang: 'Tình trạng',
			danglam: 'Đang làm',
			daduyet: 'Đã duyệt',
			choduyet: 'Chờ duyệt',
			duyetdangky: 'Duyệt đăng ký',
			duyet: 'Duyệt',
			tralai: 'Trả lại',
			lydo: 'Lý do',
		},
		viewsetting:
		{
			tasklocation: 'Task locations',
			showclosedtask: 'Show closed tasks',
			showclosedsubtask: 'Show closed subtasks',
			showemptystatus: 'show empty statuses'
		},
	}
};
