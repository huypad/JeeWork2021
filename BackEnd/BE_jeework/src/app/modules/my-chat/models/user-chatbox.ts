import { Member } from "./member";

export class UserChatBox{
    user: any;
    right: number;//position

    constructor(_user, _right){
        this.user = _user;
        this.right = _right;
    }
}