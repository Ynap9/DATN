import { StatusResonse } from "../const/base.consts";

export interface IResponseList<Item> {
    message: string;
    code: number;
    data: {
        results: Item[],
        page: number,
        limit: number,
        totalPages: number,
        totalResults: number
    };
    status: string,
    statusCode: StatusResonse
}


export interface IResponseItem<Item> {
    data: Item;
    message: string;
    status: string,
    statusCode: StatusResonse
}

export interface IResponseItemArray<Item> {
    data: Item[];
    message: string;
    status: string,
    statusCode: StatusResonse
}

