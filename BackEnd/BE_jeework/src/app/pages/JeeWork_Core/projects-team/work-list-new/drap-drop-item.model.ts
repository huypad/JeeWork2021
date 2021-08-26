export class DrapDropItem {
    id_row: number;
    typedrop: number;  // 1 - Kéo từ list - sang list (Chung project) thì thay đổi vị trí, 2- kéo từ check list - list, 3 - Kéo từ status - status, 4 - kéo từ list - sang list, 5 - kéo thay đổi vị trí cột
    id_project_team: number;
    id_parent: number;
    status: number; // + 
    status_to: number; // +
    status_from: number;
    id_to: number; // thằng đc kéo +
    id_from: number; //từ nơi bị kéo +
    priority_from: number; // áp dụng case = 2 
    IsAbove: boolean; // dưới lên bằng true;
    fieldname: string; 
}
export class ColumnWorkModel
{
    id_row:number = 0;
    id_project_team :number = 0;
    id_department  :number = 0;
    columnname :string;
    isnewfield :boolean = false;
    Options :any;
    type :number;
    title:string;
}

export class OptionsModel
{
    rowid :number;
    FieldID :number;
    TypeID :number;
    ID_project_team :number;
    Value :string;
    Color:string;
    Note:string;
    constructor(){
        this.rowid = 0;
        this.FieldID = 0;
        this.TypeID = 0;
        this.ID_project_team = 0;
        this.Value = "";
        this.Color = "";
        this.Note = "";
    }
}

