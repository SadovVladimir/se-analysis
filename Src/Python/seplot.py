import sys
import os
import textwrap
from collections import namedtuple, Counter
import locale
from multiprocessing import Pool
import time
import datetime
import argparse
import shutil


import matplotlib.pyplot as plt
import matplotlib.dates as mdates
import pandas
import numpy

NamedData = namedtuple("NamedData", ["se_section", "name", "data"])

locale.setlocale(locale.LC_ALL, "")


def get_dir_name_from_path(path_to_file):
    return os.path.basename(os.path.dirname(path_to_file))


def plot_time_ser(named_data: NamedData, title):
    data = named_data.data
    data = data.sort_values("Date")

    locator = mdates.AutoDateLocator(minticks=10)
    date_fmt = mdates.DateFormatter("%Y\n%b")

    figure = plt.figure()
    plt.title("{0}\n{1}".format(title, named_data.se_section))
    plt.xlabel("Дата")
    plt.ylabel("Количество")
    plt.plot_date(data["Date"], data["Count"], fmt="ko-")
    plt.gca().xaxis.set_major_locator(locator)
    plt.gca().xaxis.set_major_formatter(date_fmt)
    plt.grid(True)

    return figure


def plot(named_data: NamedData):
    name = named_data.name
    figure = None
    if name == "Ages":
        figure = plot_ages(named_data)
    elif name == "PopTags":
        figure = plot_tags(named_data, "Популярные теги")
    elif name == "UnpopTags":
        figure = plot_tags(named_data, "Непопулярные теги")
    elif name == "UsersReg":
        figure = plot_time_ser(named_data, "Регистрации новых пользователей")
    elif name == "PostsCrDate":
        figure = plot_time_ser(named_data, "Новые вопросы")
    return figure


def plot_pop_tags_by_time(named_data: NamedData):
    data = named_data.data
    tags_by_month = data.groupby(pandas.Grouper(key="Date", freq="Y")).agg(";".join)
    x = []
    y = []
    labels = []

    plt.figure()

    n_max_tags = 5

    for row in tags_by_month.itertuples():
        x.append(row.Index)
        most_commons = Counter(row.Tags.split(";")).most_common(n_max_tags)
        y.append(sum(val[1] for val in most_commons))
        labels.append([textwrap.fill(val[0], 20) for val in most_commons])

    tr_labels = []
    for row in zip(*labels):
        tr_labels.append(row)

    figure = plt.figure()
    plt.stem(x, y)
    table = plt.table(cellText=tr_labels, rowLabels=tuple(str(i).center(10) for i in range(1, len(tr_labels) + 1)),
              cellLoc="center", bbox=[0.0, -0.5, 1, 0.3])
    plt.title("{0}\n{1}".format("Популярность тегов со временем", named_data.se_section))
    plt.xlabel("Дата")
    plt.ylabel("Суммарное количество вопросов")
    plt.gca().set_xticks(x)
    return figure


def plot_ages(named_data: NamedData):
    data_without_nan = named_data.data.dropna(axis="index")

    ages_figure = plt.figure()

    ages = (0, 17, 23, 31, 46, 66, 100)

    plt.title("Распредление по возрастам\n{}".format(named_data.se_section))
    plt.xlabel("Возраст")
    plt.ylabel("Количество")
    plt.xticks(ages)
    plt.hist(data_without_nan["Age"], bins=ages, weights=data_without_nan["Count"])

    return ages_figure


def plot_tags(named_data: NamedData, title):
    tags_fig = plt.figure()
    grouped_by_tags = named_data.data.groupby("Count", as_index=False)
    tags = grouped_by_tags.agg(", ".join).sort_values("Count")

    max_tags = min(tags.shape[0], 10)

    if named_data.name == "PopTags":
        tags = tags[-max_tags:]
    else:
        tags = tags[:max_tags]

    plt.title("{0}\n{1}".format(title, named_data.se_section))
    plt.xlabel("Количество")
    plt.ylabel("Название тегов")
    plt.barh(tags["Tag"], tags["Count"])
    plt.grid(True, axis="x")
    axes = plt.gca()
    axes.set_yticklabels((textwrap.fill(label, width=150) for label in tags["Tag"]))

    return tags_fig


def load_dataset(path_to_file):
    se_section = get_dir_name_from_path(path_to_file)
    base_name = os.path.basename(path_to_file)
    name_without_ext = os.path.splitext(base_name)[0]
    data = None

    if name_without_ext == "Ages":
        data = pandas.read_csv(path_to_file, dtype={"Age": numpy.float32, "Count": numpy.float32}, na_values="None"
                               , engine="c")
    elif name_without_ext == "PopTags" or name_without_ext == "UnpopTags":
        data = pandas.read_csv(path_to_file, dtype={"Tag": str, "Count": numpy.uint32}, keep_default_na=False)
    elif name_without_ext == "UsersReg" or name_without_ext == "PostsCrDate":
        data = pandas.read_csv(path_to_file, parse_dates={"Date": [0, 1]})
        data.sort_values("Date", inplace=True)
    if data is not None:
        print("\tLoaded: {}.".format(base_name))
    return None if data is None else NamedData(se_section, name_without_ext, data)


def plot_and_save_figure(paths_and_file):
        path_to_save, named_data = paths_and_file
        figure = plot(named_data)
        path = os.path.join(path_to_save, named_data.name)
        if figure is not None:
            plt.figure(figure.number)
            plt.tick_params(labelsize="x-small")
            figure.savefig(path + ".png", fmt="png", dpi=300, bbox_inches="tight")
            plt.close(figure)


if __name__ == "__main__":
    args_parser = argparse.ArgumentParser(description="Visualize data from work result of C# program.")
    args_parser.add_argument("-p", "--parallel", action="store_true", help="Running the program in parallel mode.")
    args_parser.add_argument("input_dir", help="A path to a directory which contains csv files."
                                               " Files are the result of work C# program.")

    cli_args = args_parser.parse_args()

    if not os.path.isdir(cli_args.input_dir):
        print("\n\t'{}' does not exist.".format(cli_args.input_dir))
        sys.exit(2)

    new_dir_img = os.path.join(".", "Images")

    if os.path.exists(new_dir_img):
        is_empty_dir = False
        with os.scandir(new_dir_img) as image_dir_it:
            if next(image_dir_it, None) is None:
                is_empty_dir = True
        if not is_empty_dir:
            print("The directory '{0}' is not empty. Need to clear the directory."
                  " Do you want to delete it?".format(new_dir_img))
            answer = 'n'
            while True:
                print("Enter 'Y/y' or 'N/n'.")
                answer = input().rstrip().lower()
                if answer == 'y' or answer == 'n':
                    break

            if answer == 'n':
                print("The program need to clear '{0}' for further work. "
                      "Save all files from '{0}' and rerun program.".format(new_dir_img))
                sys.exit(0)
            elif answer == 'y':
                shutil.rmtree(new_dir_img)
                os.mkdir(new_dir_img)
    else:
        os.mkdir(new_dir_img)

    paths_and_datasets = []

    with os.scandir(cli_args.input_dir) as dirit:
        for direct in dirit:
            if direct.is_dir():
                if direct.name != "Images":
                    print("Scan dir: {}".format(direct.name))
                    img_res_dir = os.path.join(new_dir_img, direct.name)
                    if not os.path.exists(img_res_dir):
                        os.mkdir(img_res_dir)
                    with os.scandir(direct.path) as fileit:
                        for file in fileit:
                            if file.is_file(follow_symlinks=False):
                                if file.name.endswith(".csv"):
                                    data = load_dataset(file.path)
                                    if data is not None:
                                        paths_and_datasets.append((img_res_dir, data))

    print("The loading completed.")
    print("Drawing...")
    start_time = time.perf_counter()
    if cli_args.parallel:
        with Pool() as pool:
            pool.map(plot_and_save_figure, paths_and_datasets)
    else:
        for val in paths_and_datasets:
            plot_and_save_figure(val)
    end_time = time.perf_counter()

    print("Elapsed time: {0} S.".format(end_time - start_time))

